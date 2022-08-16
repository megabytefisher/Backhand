using Backhand.DeviceIO.Cmp;
using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpTransports;
using Backhand.DeviceIO.NetSync;
using Backhand.DeviceIO.Padp;
using Backhand.DeviceIO.Slp;
using Backhand.DeviceIO.Usb;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public class UsbDlpServer : DlpServer
    {
        private class UsbDlpClient
        {
            public string DevicePath { get; }
            public Task HandlerTask { get; }

            public UsbDlpClient(string devicePath, Task handlerTask)
            {
                DevicePath = devicePath;
                HandlerTask = handlerTask;
            }
        }

        private readonly List<UsbDlpClient> _activeClients;

        public UsbDlpServer(Func<DlpContext, CancellationToken, Task> syncFunc, ILoggerFactory? loggerFactory = null)
            : base(syncFunc, loggerFactory)
        {
            _activeClients = new List<UsbDlpClient>();
        }

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                MaintainClients();

                (UsbRegistry? newDeviceInfo, UsbDeviceConfig? config) newDevice = UsbDevice.AllDevices
                    .Select(di => (deviceInfo: di, deviceConfig: UsbDeviceConfigs.Devices.GetValueOrDefault(((ushort)di.Vid, (ushort)di.Pid))))
                    .Where(d => d.deviceConfig != null)
                    .Where(d => !_activeClients.Any(c => c.DevicePath == d.deviceInfo.DevicePath))
                    .FirstOrDefault();

                if (newDevice.newDeviceInfo != null)
                {
                    _activeClients.Add(new UsbDlpClient(newDevice.newDeviceInfo.DevicePath, HandleDevice(newDevice.newDeviceInfo, newDevice.config!, cancellationToken)));
                }

                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }

        private void MaintainClients()
        {
            _activeClients.RemoveAll(c => c.HandlerTask.IsCompleted);
        }

        private async Task HandleDevice(UsbRegistry info, UsbDeviceConfig config, CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource abortCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(abortCts.Token, cancellationToken);

            bool openSuccess = info.Open(out UsbDevice usbDevice);
            if (!openSuccess)
                throw new DlpException("Couldn't open USB device");
            
            try
            {
                (ReadEndpointID readEndpoint, WriteEndpointID writeEndpoint) = UsbHandshake.DoHardwareHandshake(usbDevice, config.HandshakeType);

                Task? syncTask;
                if (config.ProtocolType == UsbProtocolType.NetSync)
                {
                    UsbDevicePipe usbDevicePipe = new(usbDevice, readEndpoint, writeEndpoint);
                    NetSyncDevice netSyncDevice = new(usbDevicePipe);
                    NetSyncConnection netSyncConnection = new(netSyncDevice);
                    
                    // Start handshake early so it sees the wakeup packet.
                    Task netSyncHandshakeTask = netSyncConnection.DoHandshakeAsync(cancellationToken);

                    Task deviceIoTask = usbDevicePipe.RunIoAsync(linkedCts.Token);
                    Task netSyncIoTask = netSyncDevice.RunIoAsync(linkedCts.Token);

                    await netSyncHandshakeTask;

                    NetSyncDlpTransport transport = new(netSyncConnection);
                    DlpConnection dlpConnection = new(transport);
                    DlpContext dlpContext = new(dlpConnection);

                    syncTask = DoSyncAsync(dlpContext, linkedCts.Token);

                    try
                    {
                        await Task.WhenAny(deviceIoTask, netSyncIoTask, syncTask).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore any exceptions for now - we want all tasks to end.
                    }

                    abortCts.Cancel();

                    try
                    {
                        await Task.WhenAll(deviceIoTask, netSyncIoTask, syncTask).ConfigureAwait(false);
                    }
                    catch
                    {
                        if (syncTask.IsCompletedSuccessfully)
                        {
                            // Swallow
                        }
                        else
                        {
                            // Do something
                        }
                    }
                }
                else if (config.ProtocolType == UsbProtocolType.Slp)
                {
                    UsbDevicePipe usbDevicePipe = new(usbDevice, readEndpoint, writeEndpoint);
                    using SlpDevice slpDevice = new(usbDevicePipe);
                    PadpConnection padpConnection = new(slpDevice, 3, 3);

                    // Watch for wakeup packet(?)
                    CmpConnection cmpConnection = new(padpConnection);
                    Task waitForWakeupTask = cmpConnection.WaitForWakeUpAsync(cancellationToken);

                    Task deviceIoPipe = usbDevicePipe.RunIoAsync(cancellationToken);
                    Task slpIoTask = slpDevice.RunIoAsync(cancellationToken);
                    await waitForWakeupTask.ConfigureAwait(false);

                    await cmpConnection.DoHandshakeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                    PadpDlpTransport transport = new(padpConnection);
                    DlpConnection dlpConnection = new(transport);
                    DlpContext dlpContext = new(dlpConnection);

                    syncTask = DoSyncAsync(dlpContext, linkedCts.Token);

                    try
                    {
                        await Task.WhenAny(deviceIoPipe, slpIoTask, syncTask).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore any exceptions for now - we want all tasks to end.
                    }

                    abortCts.Cancel();

                    try
                    {
                        await Task.WhenAll(deviceIoPipe, slpIoTask, syncTask).ConfigureAwait(false);
                    }
                    catch
                    {
                        if (syncTask.IsCompletedSuccessfully)
                        {
                            // Swallow
                        }
                        else
                        {
                            // Do something
                        }
                    }
                }
            }
            finally
            {
                usbDevice.Close();
            }

            // Don't allow immediate reconnection
            await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
        }
    }
}
