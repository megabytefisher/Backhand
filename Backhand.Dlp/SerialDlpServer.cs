using Backhand.Common.Pipelines;
using Backhand.Protocols.Cmp;
using Backhand.Protocols.Dlp;
using Backhand.Protocols.Padp;
using Backhand.Protocols.Slp;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public class SerialDlpServer : DlpServer
    {
        private readonly string _portName;
        private readonly bool _singleSync;

        private const int InitialBaudRate = 9600;
        private const int TargetBaudRate = 9600;

        public SerialDlpServer(DlpSyncFunc syncFunc, string portName, bool singleSync = false, CancellationToken cancellationToken = default) : base(syncFunc)
        {
            _portName = portName;
            _singleSync = singleSync;
        }

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await DoCmpPortionAsync(cancellationToken).ConfigureAwait(false);
                    await DoDlpPortionAsync(cancellationToken).ConfigureAwait(false);

                    if (_singleSync)
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

            using SlpConnection slp = new SlpConnection(serialPipe);
            PadpConnection padp = new(slp, 3, 3);

            Task? waitForWakeUpTask = null;
            Task? ioTask = null;
            Task? ioCompletionTask = null;
            List<Task> tasks = new List<Task>(3);
            try
            {
                // Watch for wakeup
                waitForWakeUpTask = CmpConnection.WaitForWakeUpAsync(padp, linkedCts.Token);
                tasks.Add(waitForWakeUpTask);

                // Run SLP IO
                ioTask = slp.RunIOAsync(linkedCts.Token);
                tasks.Add(ioTask);

                // 
                ioCompletionTask = CancelOnCompletion(ioTask, internalCts);
                tasks.Add(ioCompletionTask);

                // Wait for wakeup + do handshake
                await waitForWakeUpTask.ConfigureAwait(false);
                await CmpConnection.DoHandshakeAsync(padp, TargetBaudRate, cancellationToken: cancellationToken).ConfigureAwait(false);
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

        private async Task DoDlpPortionAsync(CancellationToken cancellationToken = default)
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

            using SlpConnection slp = new(serialPipe);
            PadpConnection padp = new(slp, 3, 3);

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
                DlpConnection dlp = new(padp);

                // Do sync
                await DoSyncAsync(dlp, linkedCts.Token).ConfigureAwait(false);
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
