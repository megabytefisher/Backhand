using LibUsbDotNet;
using LibUsbDotNet.Main;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Backhand.DeviceIO.Slp
{
    public class UsbSlpDevice : SlpDevice
    {
        // USB device settings
        private UsbDevice _usbDevice;
        private ReadEndpointID _readEndpointId;
        private WriteEndpointID _writeEndpointId;

        public UsbSlpDevice(UsbDevice usbDevice, ReadEndpointID readEndpointId, WriteEndpointID writeEndpointId, ILogger? logger = null)
            : base(logger)
        {
            _usbDevice = usbDevice;
            _readEndpointId = readEndpointId;
            _writeEndpointId = writeEndpointId;
        }

        public override async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource abortCts = new CancellationTokenSource();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, abortCts.Token);

            Task readerTask = RunReaderAsync(cancellationToken);
            Task writerTask = RunWriterAsync(cancellationToken);

            try
            {
                await Task.WhenAny(readerTask, writerTask).ConfigureAwait(false);
            }
            catch
            {
            }

            abortCts.Cancel();

            await Task.WhenAll(readerTask, writerTask);
        }

        private async Task RunReaderAsync(CancellationToken cancellationToken = default)
        {
            Pipe readerPipe = new Pipe();
            await Task.WhenAll(
                FillReadPipeAsync(readerPipe.Writer, cancellationToken),
                ProcessReadPipeAsync(readerPipe.Reader, cancellationToken));
        }

        private async Task FillReadPipeAsync(PipeWriter writer, CancellationToken cancellationToken = default)
        {
            const int minimumBufferSize = 512;
            byte[] readBuffer = ArrayPool<byte>.Shared.Rent(minimumBufferSize);

            using UsbEndpointReader usbReader = _usbDevice.OpenEndpointReader(_readEndpointId);

            while (!cancellationToken.IsCancellationRequested)
            {
                ErrorCode errorCode = usbReader.SubmitAsyncTransfer(readBuffer, 0, readBuffer.Length, int.MaxValue, out UsbTransfer transferContext);
                if (errorCode != ErrorCode.Ok)
                    throw new SlpException("Failed to submit USB read transfer");

                int bytesRead = 0;

                using (transferContext)
                using (cancellationToken.Register(() =>
                {
                    transferContext.Cancel();
                }))
                {
                    await Task.Run(() =>
                    {
                        ErrorCode errorCode = transferContext.Wait(out bytesRead);
                        if (errorCode != ErrorCode.Ok)
                            throw new SlpException("Failed to complete USB read transfer");
                    }).ConfigureAwait(false);
                }

                Memory<byte> pipeBuffer = writer.GetMemory(bytesRead);
                ((Span<byte>)readBuffer).Slice(0, bytesRead).CopyTo(pipeBuffer.Span);
                writer.Advance(bytesRead);

                FlushResult flushResult = await writer.FlushAsync().ConfigureAwait(false);
                if (flushResult.IsCompleted)
                    break;
            }

            ArrayPool<byte>.Shared.Return(readBuffer);
            await writer.CompleteAsync().ConfigureAwait(false);
        }

        private async Task ProcessReadPipeAsync(PipeReader reader, CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult readResult = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = readResult.Buffer;

                SequencePosition processedPosition = ReadPackets(buffer);

                reader.AdvanceTo(processedPosition, buffer.End);

                if (readResult.IsCompleted)
                    break;
            }

            await reader.CompleteAsync().ConfigureAwait(false);
        }

        private async Task RunWriterAsync(CancellationToken cancellationToken = default)
        {
            using UsbEndpointWriter usbWriter = _usbDevice.OpenEndpointWriter(_writeEndpointId);

            while (!cancellationToken.IsCancellationRequested)
            {
                using SlpSendJob sendJob = await _sendQueue.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                ErrorCode errorCode = usbWriter.SubmitAsyncTransfer(sendJob.Buffer, 0, sendJob.Length, int.MaxValue, out UsbTransfer transferContext);
                if (errorCode != ErrorCode.Ok)
                    throw new SlpException("Failed to submit USB write transfer");

                using (transferContext)
                using (cancellationToken.Register(() =>
                {
                    transferContext.Cancel();
                }))
                {
                    await Task.Run(() =>
                    {
                        ErrorCode errorCode = transferContext.Wait(out int wroteCount);
                        if (errorCode != ErrorCode.Ok)
                            throw new SlpException("Failed to complete USB write transfer");
                    }).ConfigureAwait(false);
                }
            }
        }
    }
}
