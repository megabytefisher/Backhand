using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Protocols.Cmp;
using Backhand.Protocols.Dlp;
using Backhand.Protocols.NetSync;
using Backhand.Protocols.Padp;
using Backhand.Protocols.Slp;
using Backhand.Usb;
using LibUsbDotNet.Main;
using Microsoft.Extensions.Logging;

namespace Backhand.Dlp
{
    public class UsbDlpServer<TContext> : DlpServer<TContext>
    {
        private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(1000);

        public UsbDlpServer(ILoggerFactory? loggerFactory = null)
            : base(loggerFactory)
        {
        }

        public override async Task RunAsync(ISyncHandler<TContext> syncHandler, CancellationToken cancellationToken)
        {
            await RunAsync(syncHandler, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task RunAsync(ISyncHandler<TContext> syncHandler, bool singleSync, CancellationToken cancellationToken = default)
        {
            Dictionary<string, UsbDlpClient> clients = new();

            using CancellationTokenSource innerCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCts.Token);

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    foreach (string path in clients.Keys.ToList())
                    {
                        if (clients[path].HandlerTask.IsCompleted)
                        {
                            clients.Remove(path);
                        }
                    }

                    foreach ((UsbDeviceDescriptor device, UsbDeviceConfig config) in UsbDeviceConfigs.GetAvailableDevices())
                    {
                        if (clients.ContainsKey(device.DevicePath))
                        {
                            continue;
                        }

                        UsbDlpClient client = new(device.Open(), config, (connection, cancellation) => HandleConnection(connection, syncHandler, cancellation), innerCts.Token, LoggerFactory);
                        clients.Add(device.DevicePath, client);

                        if (singleSync)
                        {
                            break;
                        }    
                    }

                    if (singleSync && clients.Any())
                    {
                        await Task.WhenAll(clients.Values.Select(c => c.HandlerTask)).ConfigureAwait(false);
                        break;
                    }

                    await Task.Delay(PollDelay, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                innerCts.Cancel();
                try
                {
                    await Task.WhenAll(clients.Values.Select(c => c.HandlerTask)).ConfigureAwait(false);
                }
                catch { /* Swallow */ }

                foreach (UsbDlpClient client in clients.Values)
                {
                    client.Dispose();
                }
            }
        }

        private class UsbDlpClient : IDisposable
        {
            public UsbDeviceConnection Device { get; }
            public UsbDeviceConfig Config { get; }
            public Func<DlpConnection, CancellationToken, Task> SyncFunc { get; }
            public Task HandlerTask { get; }
            public CancellationTokenSource CancellationTokenSource { get; }

            private CancellationToken _externalCancellationToken;

            private readonly ILogger _netSyncInterfaceLogger;
            private readonly ILogger _netSyncConnectionLogger;
            private readonly ILogger _slpInterfaceLogger;
            private readonly ILogger _padpConnectionLogger;
            private readonly ILogger _cmpConnectionLogger;
            private readonly ILogger _dlpConnectionLogger;

            public UsbDlpClient(UsbDeviceConnection device, UsbDeviceConfig config, Func<DlpConnection, CancellationToken, Task> syncFunc, CancellationToken cancellationToken, ILoggerFactory loggerFactory)
            {
                Device = device;
                Config = config;
                SyncFunc = syncFunc;
                CancellationTokenSource = new CancellationTokenSource();
                _externalCancellationToken = cancellationToken;

                HandlerTask = Task.Run(HandleDeviceAsync);

                _netSyncInterfaceLogger = loggerFactory.CreateLogger<NetSyncInterface>();
                _netSyncConnectionLogger = loggerFactory.CreateLogger<NetSyncConnection>();
                _slpInterfaceLogger = loggerFactory.CreateLogger<SlpInterface>();
                _padpConnectionLogger = loggerFactory.CreateLogger<PadpConnection>();
                _cmpConnectionLogger = loggerFactory.CreateLogger<CmpConnection>();
                _dlpConnectionLogger = loggerFactory.CreateLogger<DlpConnection>();
            }

            public void Dispose()
            {
                Device.Dispose();
                CancellationTokenSource.Cancel();
                CancellationTokenSource.Dispose();
            }

            private async Task HandleDeviceAsync()
            {
                using CancellationTokenSource innerCts = new();
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_externalCancellationToken, CancellationTokenSource.Token, innerCts.Token);

                (ReadEndpointID readEndpoint, WriteEndpointID writeEndpoint) = UsbHandshake.DoHandshake(Device, Config.HandshakeMode);

                using UsbDevicePipe pipe = new(Device, readEndpoint, writeEndpoint);
                Task pipeTask = pipe.RunIOAsync(linkedCts.Token);

                try
                {
                    switch (Config.ProtocolType)
                    {
                        case UsbProtocolType.NetSync:
                            await HandleNetSyncDeviceAsync(pipe, SyncFunc, linkedCts.Token).ConfigureAwait(false);
                            break;
                        case UsbProtocolType.Slp:
                            await HandleSlpDeviceAsync(pipe, SyncFunc, linkedCts.Token).ConfigureAwait(false);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                finally
                {
                    innerCts.Cancel();
                    try
                    {
                        await pipeTask.ConfigureAwait(false);
                    }
                    catch
                    {
                        // Swallow.
                    }
                }
            }

            private async Task HandleNetSyncDeviceAsync(UsbDevicePipe devicePipe, Func<DlpConnection, CancellationToken, Task> syncFunc, CancellationToken cancellationToken)
            {
                using CancellationTokenSource innerCts = new();
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCts.Token);

                using NetSyncInterface netSyncInterface = new(devicePipe, logger: _netSyncInterfaceLogger);
                NetSyncConnection netSyncConnection = new(netSyncInterface, logger: _netSyncConnectionLogger);

                Task? netSyncHandshakeTask = null;
                Task? netSyncIoTask = null;
                List<Task> tasks = new List<Task>(3);
                try
                {
                    // Start the handshake task early enough to see the wakeup packet
                    netSyncHandshakeTask = netSyncConnection.DoHandshakeAsync(linkedCts.Token);
                    tasks.Add(netSyncHandshakeTask);
                    
                    // Start IO tasks
                    netSyncIoTask = netSyncInterface.RunIOAsync(linkedCts.Token);
                    tasks.Add(netSyncIoTask);

                    // Wait for the handshake task to complete
                    await netSyncHandshakeTask.ConfigureAwait(false);

                    // Build up the DLP connection
                    DlpConnection dlpConnection = new(netSyncConnection, logger: _dlpConnectionLogger);
                    await syncFunc(dlpConnection, linkedCts.Token);
                }
                finally
                {
                    innerCts.Cancel();
                    try
                    {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }

            private async Task HandleSlpDeviceAsync(UsbDevicePipe devicePipe, Func<DlpConnection, CancellationToken, Task> syncFunc, CancellationToken cancellationToken)
            {
                using CancellationTokenSource innerCts = new CancellationTokenSource();
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCts.Token);

                using SlpInterface slpInterface = new(devicePipe, logger: _slpInterfaceLogger);
                PadpConnection padpConnection = new(slpInterface, 3, 3, logger: _padpConnectionLogger);

                Task? waitForWakeUpTask = null;
                Task? ioTask = null;
                List<Task> tasks = new List<Task>(3);
                try
                {
                    CmpConnection cmpConnection = new(padpConnection, logger: _cmpConnectionLogger);

                    // Watch for wakeup
                    waitForWakeUpTask = cmpConnection.WaitForWakeUpAsync(linkedCts.Token);
                    tasks.Add(waitForWakeUpTask);

                    // Run SLP IO
                    ioTask = slpInterface.RunIOAsync(linkedCts.Token);
                    tasks.Add(ioTask);

                    // Wait for wakeup + do handshake
                    await waitForWakeUpTask.ConfigureAwait(false);
                    await cmpConnection.DoHandshakeAsync(cancellationToken: linkedCts.Token).ConfigureAwait(false);

                    // Build up the DLP connection
                    DlpConnection dlpConnection = new(padpConnection, logger: _dlpConnectionLogger);

                    // Run the sync function
                    await syncFunc(dlpConnection, linkedCts.Token).ConfigureAwait(false);
                }
                finally
                {
                    innerCts.Cancel();
                    try
                    {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Swallow
                    }
                }
            }
        }
    }
}