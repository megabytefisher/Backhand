using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpTransports;
using Backhand.DeviceIO.Usb;
using LibUsbDotNet;
using LibUsbDotNet.Main;
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

        //private readonly Guid WinUsbGuid = new Guid("dee824ef-729b-4a0e-9c14-b7117d33a817");

        public UsbDlpServer(Func<DlpConnection, CancellationToken, Task> syncFunc)
            : base(syncFunc)
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
                    .SingleOrDefault();


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
            Task? syncTask = null;

            using CancellationTokenSource abortCts = new CancellationTokenSource();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(abortCts.Token, cancellationToken);

            UsbNetSyncDevice netSyncDevice = new UsbNetSyncDevice(info);

            Task ioTask = netSyncDevice.RunIOAsync(linkedCts.Token);
            await netSyncDevice.DoNetSyncHandshake().ConfigureAwait(false);

            using NetSyncDlpTransport transport = new NetSyncDlpTransport(netSyncDevice);
            DlpConnection dlpConnection = new DlpConnection(transport);

            syncTask = DoSync(dlpConnection, linkedCts.Token);

            try
            {
                await Task.WhenAny(syncTask, ioTask).ConfigureAwait(false);
            }
            catch
            {
            }

            abortCts.Cancel();

            try
            {
                await Task.WhenAll(syncTask, ioTask).ConfigureAwait(false);
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

            // Don't allow immediate reconnection
            await Task.Delay(2000);
        }
    }
}
