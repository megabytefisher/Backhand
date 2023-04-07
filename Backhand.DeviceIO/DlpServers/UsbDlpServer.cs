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

        public UsbDlpServer(Func<DlpClientContext, CancellationToken, Task> syncFunc, ILoggerFactory? loggerFactory = null)
            : base(syncFunc, loggerFactory)
        {
            _activeClients = new List<UsbDlpClient>();
        }

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                MaintainClients();

                // TODO : fix this mess
                (UsbRegistry? newDeviceInfo, UsbDeviceConfig? config) newDevice = UsbDevice.AllDevices
                    .Select(di => (deviceInfo: di, deviceConfig: UsbDeviceConfigs.Devices.GetValueOrDefault(((ushort)di.Vid, (ushort)di.Pid))))
                    .Where(d => d.deviceConfig != null)
                    .Where(d => !_activeClients.Any(c => c.DevicePath == d.deviceInfo.DevicePath))
                    .FirstOrDefault();

                if (newDevice.newDeviceInfo != null)
                {
                    _activeClients.Add(new UsbDlpClient(
                        newDevice.newDeviceInfo.DevicePath, 
                        HandleDeviceAsync(newDevice.newDeviceInfo, newDevice.config!, cancellationToken)));
                }

                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
        }

        private void MaintainClients()
        {
            _activeClients.RemoveAll(c => c.HandlerTask.IsCompleted);
        }

        private async Task HandleDeviceAsync(UsbRegistry info, UsbDeviceConfig config, CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource abortCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(abortCts.Token, cancellationToken);

            bool openSuccess = info.Open(out UsbDevice usbDevice);
            if (!openSuccess)
                throw new DlpServerException("Couldn't open USB device");
            
            try
            {
                (ReadEndpointID readEndpoint, WriteEndpointID writeEndpoint) = UsbHandshake.DoHardwareHandshake(usbDevice, config.HandshakeType);
                
                UsbDevicePipe usbDevicePipe = new(usbDevice, readEndpoint, writeEndpoint);

                switch (config.ProtocolType)
                {
                    case UsbProtocolType.NetSync:
                        await HandleNetSyncDeviceAsync(usbDevicePipe, cancellationToken).ConfigureAwait(false);
                        break;
                    case UsbProtocolType.Slp:
                        await HandleSlpDeviceAsync(usbDevicePipe, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                usbDevice.Close();
            }

            // Don't allow immediate reconnection
            await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleNetSyncDeviceAsync(UsbDevicePipe devicePipe, CancellationToken cancellationToken)
        {
            using CancellationTokenSource abortCts = new();
            using CancellationTokenSource linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(abortCts.Token, cancellationToken);
            
            NetSyncDevice netSyncDevice = new(devicePipe);
            NetSyncConnection netSyncConnection = new(netSyncDevice);
            
            // Start handshake task early enough to see the wakeup packet
            Task netSyncHandshakeTask = netSyncConnection.DoHandshakeAsync(linkedCts.Token);
            
            // Start IO tasks
            Task pipeIoTask = devicePipe.RunIoAsync(linkedCts.Token);
            Task deviceIoTask = netSyncDevice.RunIoAsync(linkedCts.Token);

            // Ideally, wait for handshake task to complete.
            try
            {
                await Task.WhenAny(netSyncHandshakeTask, deviceIoTask, pipeIoTask).ConfigureAwait(false);

                if (!netSyncHandshakeTask.IsCompleted)
                {
                    throw new DlpServerException("Device/connection task ended before NetSync handshake");
                }
            }
            catch
            {
                abortCts.Cancel();

                try
                {
                    await Task.WhenAll(netSyncHandshakeTask, deviceIoTask, pipeIoTask).ConfigureAwait(false);
                }
                catch
                {
                    // Ignored
                }

                throw;
            }
            
            // Build up DlpContext and do sync
            NetSyncDlpTransport dlpTransport = new(netSyncConnection);
            DlpConnection dlpConnection = new(dlpTransport);
            DlpClientContext dlpClientContext = new(dlpConnection);

            Task syncTask = DoSyncAsync(dlpClientContext, linkedCts.Token);
            
            // Ideally, wait for sync task to complete.
            Exception? syncException = null;
            try
            {
                await Task.WhenAny(syncTask, deviceIoTask, pipeIoTask).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                syncException = ex;
            }

            // Stop tasks
            abortCts.Cancel();

            try
            {
                await Task.WhenAll(syncTask, deviceIoTask, pipeIoTask).ConfigureAwait(false);
            }
            catch
            {
                // Ignored
            }

            if (!syncTask.IsCompletedSuccessfully && syncException != null)
            {
                throw syncException;
            }
        }

        private async Task HandleSlpDeviceAsync(UsbDevicePipe devicePipe, CancellationToken cancellationToken)
        {
            using CancellationTokenSource abortCts = new();
            using CancellationTokenSource linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(abortCts.Token, cancellationToken);

            using SlpDevice slpDevice = new(devicePipe);
            PadpConnection padpConnection = new(slpDevice, 3, 3);
            
            // Watch for wakeup packet.
            CmpConnection cmpConnection = new(padpConnection);
            Task waitForWakeupTask = cmpConnection.WaitForWakeUpAsync(linkedCts.Token);
            
            // Start IO tasks
            Task pipeIoTask = devicePipe.RunIoAsync(linkedCts.Token);
            Task deviceIoTask = slpDevice.RunIoAsync(linkedCts.Token);

            // Ideally, wait to receive a wakeup packet, then send handshake.
            Task? handshakeTask = null;
            try
            {
                await Task.WhenAny(waitForWakeupTask, pipeIoTask, deviceIoTask).ConfigureAwait(false);

                if (!waitForWakeupTask.IsCompleted)
                    throw new DlpServerException("IO task ended before wakeup received");

                handshakeTask = cmpConnection.DoHandshakeAsync(cancellationToken: linkedCts.Token);
                await Task.WhenAny(handshakeTask, pipeIoTask, deviceIoTask).ConfigureAwait(false);

                if (!handshakeTask.IsCompleted)
                    throw new DlpServerException("IO task ended before handshake completed");
            }
            catch
            {
                abortCts.Cancel();

                try
                {
                    await Task.WhenAll(waitForWakeupTask, handshakeTask ?? Task.CompletedTask,
                        pipeIoTask, deviceIoTask).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore
                }

                throw;
            }
            
            // Build up DlpContext and do sync
            PadpDlpTransport dlpTransport = new(padpConnection);
            DlpConnection dlpConnection = new(dlpTransport);
            DlpClientContext dlpClientContext = new(dlpConnection);

            Task syncTask = DoSyncAsync(dlpClientContext, linkedCts.Token);
            
            // Ideally, wait for sync task to complete.
            Exception? syncException = null;
            try
            {
                await Task.WhenAny(syncTask, deviceIoTask, pipeIoTask).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                syncException = ex;
            }

            // Stop tasks
            abortCts.Cancel();

            try
            {
                await Task.WhenAll(syncTask, deviceIoTask, pipeIoTask).ConfigureAwait(false);
            }
            catch
            {
                // Ignored
            }

            if (!syncTask.IsCompletedSuccessfully && syncException != null)
            {
                throw syncException;
            }
        }
    }
}
