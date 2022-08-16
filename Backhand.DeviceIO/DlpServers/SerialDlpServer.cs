using Backhand.DeviceIO.Cmp;
using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpTransports;
using Backhand.DeviceIO.Padp;
using Backhand.DeviceIO.Serial;
using Backhand.DeviceIO.Slp;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public class SerialDlpServer : DlpServer
    {
        private readonly string _portName;

        private const int InitialBaudRate = 9600;
        private const int TargetBaudRate = 57600;

        public SerialDlpServer(string portName, Func<DlpContext, CancellationToken, Task> syncFunc, ILoggerFactory? loggerFactory = null)
            : base(syncFunc, loggerFactory)
        {
            _portName = portName;
        }

        public override async Task RunAsync(CancellationToken cancellationToken = default)
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
                    // ignored
                }
            }
        }

        private async Task DoCmpPortion(CancellationToken cancellationToken)
        {
            using CancellationTokenSource abortCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(abortCts.Token, cancellationToken);

            using SerialPort serialPort = new(_portName);
            serialPort.BaudRate = InitialBaudRate;
            serialPort.Handshake = Handshake.RequestToSend;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Open();

            SerialPortPipe serialPortPipe = new(serialPort);
            
            using SlpDevice slpDevice = new(serialPortPipe, logger: LoggerFactory.CreateLogger<SlpDevice>());
            PadpConnection padpConnection = new(slpDevice, 3, 3, logger: LoggerFactory.CreateLogger<PadpConnection>());

            // Watch for wakeup packet
            CmpConnection cmpConnection = new(padpConnection);
            Task waitForWakeUpTask = cmpConnection.WaitForWakeUpAsync(linkedCts.Token);

            // Start device IO
            Task ioTask = slpDevice.RunIoAsync(linkedCts.Token);

            // Wait for wakeup + do handshake
            await waitForWakeUpTask.ConfigureAwait(false);
            Task handshakeTask = cmpConnection.DoHandshakeAsync(TargetBaudRate, linkedCts.Token);

            try
            {
                await Task.WhenAny(handshakeTask, ioTask).ConfigureAwait(false);
            }
            catch
            {
                // Ignore any exceptions for now - we want both tasks to end.
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
            using CancellationTokenSource abortCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(abortCts.Token, cancellationToken);

            using SerialPort serialPort = new(_portName);
            serialPort.BaudRate = TargetBaudRate;
            serialPort.Handshake = Handshake.RequestToSend;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Open();

            SerialPortPipe serialPortPipe = new(serialPort);
            
            using SlpDevice slpDevice = new(serialPortPipe, logger: LoggerFactory.CreateLogger<SlpDevice>());
            PadpConnection padpConnection = new(slpDevice, 3, 3, logger: LoggerFactory.CreateLogger<PadpConnection>());

            // Start device IO
            Task ioTask = slpDevice.RunIoAsync(linkedCts.Token);

            // Create DLP connection
            PadpDlpTransport dlpTransport = new(padpConnection);
            DlpConnection dlpConnection = new(dlpTransport);
            DlpContext dlpContext = new(dlpConnection);

            // Do sync
            Task syncTask = DoSyncAsync(dlpContext, linkedCts.Token);

            // Wait for either sync or IO task to complete/fail
            try
            {
                await Task.WhenAny(syncTask, ioTask).ConfigureAwait(false);
            }
            catch
            {
                // Ignore any exceptions for now - we want both tasks to end.
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
