using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Backhand.DeviceIO.Utility;
using Backhand.Utility.Buffers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backhand.DeviceIO.Slp
{
    public class SerialSlpDevice : SlpDevice
    {
        // Serial port settings
        private string _serialPortName;
        private int _baudRate;

        // Logging
        private long _logReadSkip = 0;

        public SerialSlpDevice(string serialPortName, int baudRate = 9600, ILogger? logger = null)
            : base(logger)
        {
            _serialPortName = serialPortName;
            _baudRate = baudRate;
        }

        public override async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            using SerialPort serialPort = new SerialPort(_serialPortName);
            serialPort.BaudRate = _baudRate;
            serialPort.Handshake = Handshake.RequestToSend;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Open();

            PipeReader serialPortReader = PipeReader.Create(serialPort.BaseStream);
            PipeWriter serialPortWriter = PipeWriter.Create(serialPort.BaseStream);

            using CancellationTokenSource abortCts = new CancellationTokenSource();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, abortCts.Token);

            Task readerTask = RunReaderAsync(serialPortReader, linkedCts.Token);
            Task writerTask = RunWriterAsync(serialPortWriter, linkedCts.Token);

            // Fixes for CancellationToken not working..
            using (linkedCts.Token.Register(() =>
            {
                serialPort.Dispose();
                _sendQueue.Complete();
            }))
            {
                // Wait for either task to complete/fail
                try
                {
                    await Task.WhenAny(readerTask, writerTask).ConfigureAwait(false);
                }
                catch
                {
                }

                // Request exit
                abortCts.Cancel();

                // Wait for both tasks to complete..
                await Task.WhenAll(readerTask, writerTask).ConfigureAwait(false);
            }
        }

        private async Task RunReaderAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            bool firstPacket = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                // Attempt reading from serial port
                ReadResult readResult = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                if (readResult.IsCanceled)
                    break;

                ReadOnlySequence<byte> buffer = readResult.Buffer;
                
                // Early exit - if we don't have atleast enough bytes in the buffer for 1 packet, just try again next time..
                //if (buffer.Length < MinPacketSize)
                //{
                //    reader.AdvanceTo(buffer.Start, buffer.End);
                //    continue;
                //}

                if (_logTraceEnabled)
                {
                    ReadOnlySequence<byte> newDataSequence = buffer.Slice(_logReadSkip);
                    if (newDataSequence.Length > 0)
                    {
                        _logger.LogTrace($"Received {newDataSequence.Length} bytes: [{HexSerialization.GetHexString(newDataSequence)}]");
                    }
                }

                // Read SLP packets from the buffer
                SequencePosition processedPosition = ReadPackets(buffer, ref firstPacket);

                // Log what we processed
                if (_logTraceEnabled)
                {
                    ReadOnlySequence<byte> processedSequence = buffer.Slice(0, processedPosition);
                    _logReadSkip = buffer.Length - processedSequence.Length;
                }

                // Advance the pipe
                reader.AdvanceTo(processedPosition, buffer.End);
            }

            //await reader.CompleteAsync();
        }

        private async Task RunWriterAsync(PipeWriter writer, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                //await _sendQueue.OutputAvailableAsync(cancellationToken).ConfigureAwait(false);
                SlpSendJob sendJob = await _sendQueue.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                if (_logTraceEnabled)
                {
                    _logger.LogTrace($"Sending {sendJob.Length} bytes: [{HexSerialization.GetHexString(((Span<byte>)sendJob.Buffer).Slice(0, sendJob.Length))}]");
                }

                Memory<byte> sendBuffer = writer.GetMemory(sendJob.Length);
                sendJob.Buffer.CopyTo(sendBuffer);

                writer.Advance(sendJob.Length);
                FlushResult flushResult = await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                if (flushResult.IsCompleted)
                    break;

                // Dispose sendJob to return allocated array to pool
                sendJob.Dispose();
            }
            
            //await writer.CompleteAsync();
        }
    }
}
