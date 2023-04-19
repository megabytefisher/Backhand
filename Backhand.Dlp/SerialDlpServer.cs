using Backhand.Common.Pipelines;
using Backhand.Protocols.Cmp;
using Backhand.Protocols.Dlp;
using Backhand.Protocols.Padp;
using Backhand.Protocols.Slp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public class SerialDlpServer<TContext> : DlpServer<TContext>
    {
        private readonly string _portName;
        private readonly ILogger _slpLogger;
        private readonly ILogger _padpLogger;
        private readonly ILogger _cmpLogger;
        private readonly ILogger _dlpLogger;

        private const int InitialBaudRate = 9600;
        private const int TargetBaudRate = 57600;

        public SerialDlpServer(string portName, ILoggerFactory? loggerFactory = null)
            : base(loggerFactory)
        {
            _portName = portName;

            _slpLogger = LoggerFactory.CreateLogger<SlpInterface>();
            _padpLogger = LoggerFactory.CreateLogger<PadpConnection>();
            _cmpLogger = LoggerFactory.CreateLogger<CmpConnection>();
            _dlpLogger = LoggerFactory.CreateLogger<DlpConnection>();
        }

        public override async Task RunAsync(ISyncHandler<TContext> context, CancellationToken cancellationToken)
        {
            await RunAsync(context, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task RunAsync(ISyncHandler<TContext> syncHandler, bool singleSync, CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await DoCmpPortionAsync(cancellationToken).ConfigureAwait(false);
                    await DoDlpPortionAsync(syncHandler, cancellationToken).ConfigureAwait(false);

                    if (singleSync)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // TODO : Something
                    await Task.Delay(500).ConfigureAwait(false);
                }
            }
        }

        private async Task DoCmpPortionAsync(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource internalCts = new();
            using CancellationTokenSource linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(internalCts.Token, cancellationToken);

            using SerialPort serialPort = new(_portName);
            serialPort.BaudRate = InitialBaudRate;
            serialPort.Handshake = Handshake.RequestToSend;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Open();

            SerialPortPipe serialPipe = new(serialPort);

            using SlpInterface slpInterface = new SlpInterface(serialPipe, logger: _slpLogger);
            PadpConnection padpConnection = new(slpInterface, 3, 3, logger: _padpLogger);

            Task? waitForWakeUpTask = null;
            Task? ioTask = null;
            Task? ioCompletionTask = null;
            List<Task> tasks = new List<Task>(3);
            try
            {
                CmpConnection cmpConnection = new(padpConnection, logger: _cmpLogger);

                // Watch for wakeup
                waitForWakeUpTask = cmpConnection.WaitForWakeUpAsync(linkedCts.Token);
                tasks.Add(waitForWakeUpTask);

                // Run SLP IO
                ioTask = slpInterface.RunIOAsync(linkedCts.Token);
                tasks.Add(ioTask);

                // 
                ioCompletionTask = CancelOnCompletion(ioTask, internalCts);
                tasks.Add(ioCompletionTask);

                // Wait for wakeup + do handshake
                await waitForWakeUpTask.ConfigureAwait(false);
                await cmpConnection.DoHandshakeAsync(TargetBaudRate, cancellationToken: linkedCts.Token).ConfigureAwait(false);
            }
            finally
            {
                internalCts.Cancel();
                serialPort.Close();
                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch
                {
                    // Swallow
                }
                serialPort.Dispose();
            }
        }

        private async Task DoDlpPortionAsync(ISyncHandler<TContext> syncHandler, CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource internalCts = new();
            using CancellationTokenSource linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(internalCts.Token, cancellationToken);

            using SerialPort serialPort = new(_portName);
            serialPort.BaudRate = TargetBaudRate;
            serialPort.Handshake = Handshake.RequestToSend;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Open();

            SerialPortPipe serialPipe = new(serialPort);

            using SlpInterface slp = new(serialPipe, logger: _slpLogger);
            PadpConnection padp = new(slp, 3, 3, logger: _padpLogger);

            Task? ioTask = null;
            Task? ioCompletionTask = null;
            List<Task> tasks = new List<Task>(2);
            try
            {
                // Start device IO
                ioTask = slp.RunIOAsync(linkedCts.Token);
                tasks.Add(ioTask);

                //
                ioCompletionTask = CancelOnCompletion(ioTask, internalCts);
                tasks.Add(ioCompletionTask);

                // Create DLP connection
                DlpConnection dlp = new(padp, logger: _dlpLogger);

                // Do sync
                await HandleConnection(dlp, syncHandler, linkedCts.Token).ConfigureAwait(false);
            }
            finally
            {
                internalCts.Cancel();
                serialPort.Close();
                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch
                {
                    // Swallow
                }
                serialPort.Dispose();
            }
        }

        private async Task CancelOnCompletion(Task task, CancellationTokenSource cts)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                // 🐟 gulp 🐟
            }

            cts.Cancel();
        }
    }
}
