using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Common;
using Backhand.Protocols;
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
    public class UsbDlpServer : DlpServer
    {
        public TimeSpan PollDelay { get; init; } = TimeSpan.FromMilliseconds(1000);

        private readonly ILogger _netSyncInterfaceLogger;
        private readonly ILogger _netSyncConnectionLogger;
        private readonly ILogger _slpInterfaceLogger;
        private readonly ILogger _padpConnectionLogger;
        private readonly ILogger _cmpConnectionLogger;
        private readonly ILogger _dlpConnectionLogger;

        public UsbDlpServer(ILoggerFactory? loggerFactory = null)
            : base(loggerFactory)
        {
            _netSyncInterfaceLogger = LoggerFactory.CreateLogger(DlpServerLogging.NetSyncInterfaceCategory);
            _netSyncConnectionLogger = LoggerFactory.CreateLogger(DlpServerLogging.NetSyncConnectionCategory);
            _slpInterfaceLogger = LoggerFactory.CreateLogger(DlpServerLogging.SlpInterfaceCategory);
            _padpConnectionLogger = LoggerFactory.CreateLogger(DlpServerLogging.PadpConnectionCategory);
            _cmpConnectionLogger = LoggerFactory.CreateLogger(DlpServerLogging.CmpConnectionCategory);
            _dlpConnectionLogger = LoggerFactory.CreateLogger(DlpServerLogging.DlpConnectionCategory);
        }

        public override string ToString() => "usb";

        public override async Task RunAsync(ISyncHandler syncHandler, bool singleSync, CancellationToken cancellationToken = default)
        {
            Logger.ServerStarting(this);
            
            Dictionary<string, UsbDlpClient> clients = new();
            
            using CancellationTokenSource innerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Exception? exception = null;
            
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    foreach (KeyValuePair<string, UsbDlpClient> kvp in clients.ToArray())
                    {
                        if (kvp.Value.Task.IsCompleted)
                        {
                            clients.Remove(kvp.Key);
                        }
                    }

                    foreach ((UsbDeviceDescriptor device, UsbDeviceConfig config) in UsbDeviceConfigs.GetAvailableDevices())
                    {
                        if (clients.ContainsKey(device.DevicePath))
                        {
                            continue;
                        }

                        UsbDlpClient client = await StartClientAsync(device, config, syncHandler, innerCts.Token).ConfigureAwait(false);
                        clients.Add(device.DevicePath, client);

                        if (singleSync)
                        {
                            break;
                        }
                    }

                    if (singleSync && clients.Any())
                    {
                        await Task.WhenAll(clients.Values.Select(c => c.Task)).ConfigureAwait(false);
                        break;
                    }

                    await Task.Delay(PollDelay, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                innerCts.Cancel();
                try
                {
                    await Task.WhenAll(clients.Values.Select(c => c.Task)).ConfigureAwait(false);
                }
                catch { /* Swallow */ }

                foreach (UsbDlpClient client in clients.Values)
                {
                    await client.DisposeAsync().ConfigureAwait(false);
                }
                clients.Clear();

                innerCts.Dispose();
                
                Logger.ServerStopped(this, exception);
            }
        }
        
        private async Task HandleDeviceAsync(ISyncHandler syncHandler, UsbDeviceConnection deviceConnection, UsbDeviceConfig config, CancellationToken cancellationToken)
        {
            string connectionName = UsbDlpConnection.GetConnectionName(deviceConnection.DeviceDescriptor);
            Logger.ConnectionOpened(this, connectionName);

            Exception? exception = null;
            try
            {
                using CancellationTokenSource ioCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                (ReadEndpointID readEndpoint, WriteEndpointID writeEndpoint) = UsbHandshake.DoHandshake(deviceConnection, config.HandshakeMode);

                await using UsbDevicePipe pipe = new(deviceConnection, readEndpoint, writeEndpoint, cancellationToken: ioCts.Token);
                Task pipeIoTask = pipe.RunIOAsync(ioCts.Token);
                await using ConfiguredAsyncDisposable pipeIoDispose =
                    AsyncDisposableCallback.EnsureCompletion(pipeIoTask, ioCts).ConfigureAwait(false);
            
                switch (config.ProtocolType)
                {
                    case UsbProtocolType.NetSync:
                        await HandleNetSyncDeviceAsync(pipe, syncHandler, cancellationToken).ConfigureAwait(false);
                        break;
                    case UsbProtocolType.Slp:
                        await HandleSlpDeviceAsync(pipe, syncHandler, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                Logger.ConnectionClosed(this, connectionName, exception);
            }
        }
        
        private async Task HandleNetSyncDeviceAsync(UsbDevicePipe pipe, ISyncHandler syncHandler, CancellationToken cancellationToken)
        {
            using CancellationTokenSource ioCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            using NetSyncInterface netSyncInterface = new(pipe, logger: _netSyncInterfaceLogger);
            Task netSyncIoTask = netSyncInterface.RunIOAsync(ioCts.Token);
            await using ConfiguredAsyncDisposable netSyncIoDispose =
                AsyncDisposableCallback.EnsureCompletion(netSyncIoTask, ioCts).ConfigureAwait(false);
            
            NetSyncConnection netSyncConnection = new(netSyncInterface, logger: _netSyncConnectionLogger);
            await netSyncConnection.DoHandshakeAsync(cancellationToken).ConfigureAwait(false);
            
            DlpConnection dlpConnection = new UsbDlpConnection(pipe.Connection.DeviceDescriptor, netSyncConnection, logger: _dlpConnectionLogger);
            await SyncAsync(dlpConnection, syncHandler, cancellationToken).ConfigureAwait(false);
        }
        
        private async Task HandleSlpDeviceAsync(UsbDevicePipe pipe, ISyncHandler syncHandler, CancellationToken cancellationToken)
        {
            using CancellationTokenSource ioCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            using SlpInterface slpInterface = new(pipe, logger: _slpInterfaceLogger);
            Task slpIoTask = slpInterface.RunIOAsync(ioCts.Token);
            await using ConfiguredAsyncDisposable slpIoDispose =
                AsyncDisposableCallback.EnsureCompletion(slpIoTask, ioCts).ConfigureAwait(false);
            
            PadpConnection padpConnection = new(slpInterface, 3, 3, logger: _padpConnectionLogger);
            
            CmpConnection cmpConnection = new(padpConnection, logger: _cmpConnectionLogger);
            await cmpConnection.DoHandshakeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            
            DlpConnection dlpConnection = new UsbDlpConnection(pipe.Connection.DeviceDescriptor, padpConnection, logger: _dlpConnectionLogger);
            await SyncAsync(dlpConnection, syncHandler, cancellationToken).ConfigureAwait(false);
        }

        private async Task<UsbDlpClient> StartClientAsync(UsbDeviceDescriptor device, UsbDeviceConfig config, ISyncHandler syncHandler, CancellationToken cancellationToken)
        {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            UsbDeviceConnection connection = await Task.Run(() => UsbDeviceConnection.Open(device), cancellationToken);

            return new UsbDlpClient
            {
                Device = device,
                Connection = connection,
                Task = Task.Run(() => HandleDeviceAsync(syncHandler, connection, config, cts.Token), cts.Token),
                CancellationTokenSource = cts
            };
        }

        private sealed class UsbDlpClient : IAsyncDisposable
        {
            public required UsbDeviceDescriptor Device { get; init; }
            public required UsbDeviceConnection Connection { get; init; }
            public required Task Task { get; init; }
            public required CancellationTokenSource CancellationTokenSource { get; init; }

            public async ValueTask DisposeAsync()
            {
                CancellationTokenSource.Cancel();

                try
                {
                    await Task;
                }
                catch
                {
                    // Swallow
                }

                Connection.Dispose();
                CancellationTokenSource.Dispose();
            }
        }

        private class UsbDlpConnection : DlpConnection
        {
            public UsbDeviceDescriptor RemoteDevice { get; }

            public UsbDlpConnection(UsbDeviceDescriptor remoteDevice, IDlpTransport dlpTransport, ArrayPool<byte>? arrayPool = null, ILogger? logger = null) : base(dlpTransport, arrayPool, logger)
            {
                RemoteDevice = remoteDevice;
            }

            public static string GetConnectionName(UsbDeviceDescriptor device) => $"usb@{device.DevicePath}";
            public override string ToString() => GetConnectionName(RemoteDevice);
        }
    }
}