using Backhand.DeviceIO.Cmp;
using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpTransports;
using Backhand.DeviceIO.Padp;
using Backhand.DeviceIO.Serial;
using Backhand.DeviceIO.Slp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public class SerialDlpServer : DlpServer
    {
        private string _portName;

        private const int InitialBaudRate = 9600;
        private const int TargetBaudRate = 57600;

        public SerialDlpServer(string portName, Func<DlpConnection, CancellationToken, Task> syncFunc, ILoggerFactory? loggerFactory = null)
            : base(syncFunc, loggerFactory)
        {
            _portName = portName;
        }

        public override async Task Run(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await DoCmpPortion(cancellationToken).ConfigureAwait(false);
                    await DoDlpPortion(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                }
            }
        }

        private async Task DoCmpPortion(CancellationToken cancellationToken)
        {
            using CancellationTokenSource abortCts = new CancellationTokenSource();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(abortCts.Token, cancellationToken);

            using SerialPort serialPort = new SerialPort(_portName);
            serialPort.BaudRate = 9600;
            serialPort.Handshake = Handshake.RequestToSend;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Open();

            SerialPortPipe serialPortPipe = new SerialPortPipe(serialPort);
            using SlpDevice slpDevice = new SlpDevice(serialPortPipe, logger: _loggerFactory.CreateLogger<SlpDevice>());
            using PadpConnection padpConnection = new PadpConnection(slpDevice, 3, 3, 0xff);

            // Watch for wakeup packet
            CmpConnection cmpConnection = new CmpConnection(padpConnection);
            Task waitForWakeUpTask = cmpConnection.WaitForWakeUpAsync(linkedCts.Token);

            // Start device IO
            Task ioTask = slpDevice.RunIOAsync(linkedCts.Token);

            // Wait for wakeup + do handshake
            await waitForWakeUpTask;
            Task handshakeTask = cmpConnection.DoHandshakeAsync(57600);

            try
            {
                await Task.WhenAny(handshakeTask, ioTask).ConfigureAwait(false);
            }
            catch
            {
            }

            abortCts.Cancel();

            try
            {
                await Task.WhenAll(handshakeTask, ioTask).ConfigureAwait(false);
            }
            catch
            {
                if (handshakeTask.IsCompletedSuccessfully)
                {
                    // Swallow
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                serialPort.Close();
            }
        }

        private async Task DoDlpPortion(CancellationToken cancellationToken)
        {
            using CancellationTokenSource abortCts = new CancellationTokenSource();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(abortCts.Token, cancellationToken);

            using SerialPort serialPort = new SerialPort(_portName);
            serialPort.BaudRate = 57600;
            serialPort.Handshake = Handshake.RequestToSend;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Open();

            SerialPortPipe serialPortPipe = new SerialPortPipe(serialPort);
            using SlpDevice slpDevice = new SlpDevice(serialPortPipe, logger: _loggerFactory.CreateLogger<SlpDevice>());
            using PadpConnection padpConnection = new PadpConnection(slpDevice, 3, 3, 0xff);

            // Start device IO
            Task ioTask = slpDevice.RunIOAsync(linkedCts.Token);

            // Create DLP connection
            using PadpDlpTransport dlpTransport = new PadpDlpTransport(padpConnection);
            DlpConnection dlpConnection = new DlpConnection(dlpTransport);

            // Do sync
            Task syncTask = DoSync(dlpConnection, linkedCts.Token);

            // Wait for either sync or IO task to complete/fail
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
                    throw;
                }
            }
            finally
            {
                serialPort.Close();
            }
        }
    }
}
