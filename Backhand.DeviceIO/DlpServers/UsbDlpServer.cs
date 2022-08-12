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
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public class UsbDlpServer : DlpServer
    {
        private class UsbDlpClient
        {
            public string DevicePath { get; private init; }
            public Task HandlerTask { get; private init; }

            public UsbDlpClient(string devicePath, Task handlerTask)
            {
                DevicePath = devicePath;
                HandlerTask = handlerTask;
            }
        }

        private List<UsbDlpClient> _activeClients;

        public UsbDlpServer(Func<DlpConnection, CancellationToken, Task> syncFunc, ILoggerFactory? loggerFactory = null)
            : base(syncFunc, loggerFactory)
        {
            _activeClients = new List<UsbDlpClient>();
        }

        public override async Task Run(CancellationToken cancellationToken = default)
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
                    _activeClients.Add(new UsbDlpClient(newDevice.newDeviceInfo.DevicePath, HandleDevice(newDevice.newDeviceInfo, newDevice.config!)));
                }

                await Task.Delay(500).ConfigureAwait(false);
            }
        }

        private void MaintainClients()
        {
            _activeClients.RemoveAll(c => c.HandlerTask.IsCompleted);
        }

        private async Task HandleDevice(UsbRegistry info, UsbDeviceConfig config, CancellationToken cancellationToken = default)
        {
            Task? syncTask;

            using CancellationTokenSource abortCts = new CancellationTokenSource();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(abortCts.Token, cancellationToken);

            bool openSuccess = info.Open(out UsbDevice usbDevice);
            if (!openSuccess)
                throw new DlpException("Couldn't open USB device");
            
            try
            {
                (ReadEndpointID readEndpoint, WriteEndpointID writeEndpoint) = UsbHandshake.DoHardwareHandshake(usbDevice, config.HandshakeType);

                if (config.ProtocolType == UsbProtocolType.NetSync)
                {
                    UsbDevicePipe usbDevicePipe = new UsbDevicePipe(usbDevice, readEndpoint, writeEndpoint);
                    NetSyncDevice netSyncDevice = new NetSyncDevice(usbDevicePipe);

                    Task deviceIoTask = usbDevicePipe.RunIOAsync(linkedCts.Token);
                    Task netSyncIoTask = netSyncDevice.RunIOAsync(linkedCts.Token);
                    await netSyncDevice.DoNetSyncHandshake().ConfigureAwait(false);

                    using NetSyncDlpTransport transport = new NetSyncDlpTransport(netSyncDevice);
                    DlpConnection dlpConnection = new DlpConnection(transport);

                    syncTask = DoSync(dlpConnection, linkedCts.Token);

                    try
                    {
                        await Task.WhenAny(deviceIoTask, netSyncIoTask, syncTask).ConfigureAwait(false);
                    }
                    catch
                    {
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
                    UsbDevicePipe usbDevicePipe = new UsbDevicePipe(usbDevice, readEndpoint, writeEndpoint);
                    using SlpDevice slpDevice = new SlpDevice(usbDevicePipe);
                    using PadpConnection padpConnection = new PadpConnection(slpDevice, 3, 3, 0xff);

                    // Watch for wakeup packet(?)
                    CmpConnection cmpConnection = new CmpConnection(padpConnection);
                    Task waitForWakeupTask = cmpConnection.WaitForWakeUpAsync(cancellationToken);

                    Task deviceIoPipe = usbDevicePipe.RunIOAsync(cancellationToken);
                    Task slpIoTask = slpDevice.RunIOAsync(cancellationToken);
                    await waitForWakeupTask.ConfigureAwait(false);

                    await cmpConnection.DoHandshakeAsync().ConfigureAwait(false);

                    using PadpDlpTransport transport = new PadpDlpTransport(padpConnection);
                    DlpConnection dlpConnection = new DlpConnection(transport);

                    syncTask = DoSync(dlpConnection, linkedCts.Token);

                    try
                    {
                        await Task.WhenAny(deviceIoPipe, slpIoTask, syncTask).ConfigureAwait(false);
                    }
                    catch
                    {
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
            await Task.Delay(2000);
        }
    }
}
