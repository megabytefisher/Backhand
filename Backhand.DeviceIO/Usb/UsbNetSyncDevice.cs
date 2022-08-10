using Backhand.DeviceIO.NetSync;
using Backhand.Utility.Buffers;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Backhand.DeviceIO.Usb
{
    public class UsbNetSyncDevice : NetSyncDevice
    {
        private class SendJob : IDisposable
        {
            public byte[] Buffer { get; set; }
            public int Length { get; set; }

            public SendJob(int length)
            {
                Length = length;
                Buffer = ArrayPool<byte>.Shared.Rent(length);
            }

            public void Dispose()
            {
                ArrayPool<byte>.Shared.Return(Buffer);
            }
        }

        private UsbDevice _usbDevice;

        private ReadEndpointID _readEndpointId;
        private WriteEndpointID _writeEndpointId;

        private BufferBlock<SendJob> _sendQueue;

        public UsbNetSyncDevice(UsbDevice usbDevice, ReadEndpointID readEndpointId, WriteEndpointID writeEndpointId)
        {
            _usbDevice = usbDevice;
            _readEndpointId = readEndpointId;
            _writeEndpointId = writeEndpointId;

            _sendQueue = new BufferBlock<SendJob>();
        }

        public void Dispose()
        {
            _sendQueue.Complete();
        }

        public async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationToken.None))
            {
                Task readerTask = RunReaderAsync(_usbDevice, linkedCts.Token);
                Task writerTask = RunWriterAsync(_usbDevice, linkedCts.Token);

                await Task.WhenAll(readerTask, writerTask).ConfigureAwait(false);
            }
        }

        public override void SendPacket(NetSyncPacket packet)
        {
            // Get packet length and allocate buffer
            int packetLength = (int)GetPacketLength(packet);
            SendJob sendJob = new SendJob(packetLength);

            // Write packet
            WritePacket(packet, ((Span<byte>)sendJob.Buffer).Slice(0, packetLength));

            // Enqueue send job
            if (!_sendQueue.Post(sendJob))
            {
                throw new NetSyncException("Failed to post packet to send queue");
            }
        }

        private async Task RunReaderAsync(UsbDevice usbDevice, CancellationToken cancellationToken)
        {
            Pipe readerPipe = new Pipe();
            await Task.WhenAll(
                FillReadPipeAsync(usbDevice, readerPipe.Writer, cancellationToken),
                ProcessReadPipeAsync(readerPipe.Reader, cancellationToken)).ConfigureAwait(false);
        }

        private async Task FillReadPipeAsync(UsbDevice usbDevice, PipeWriter writer, CancellationToken cancellationToken)
        {
            const int minimumBufferSize = 1024;
            byte[] readBuffer = ArrayPool<byte>.Shared.Rent(minimumBufferSize);

            using UsbEndpointReader usbReader = usbDevice.OpenEndpointReader(_readEndpointId);

            while (!cancellationToken.IsCancellationRequested)
            {
                ErrorCode errorCode = usbReader.SubmitAsyncTransfer(readBuffer, 0, minimumBufferSize, int.MaxValue, out UsbTransfer transferContext);
                if (errorCode != ErrorCode.Ok)
                    throw new NetSyncException("Failed to submit read transfer on USB device");

                int bytesRead;

                using (transferContext)
                using (cancellationToken.Register(() =>
                {
                    transferContext.Cancel();
                }))
                {
                    bytesRead = await Task.Run(() =>
                    {
                        var test = cancellationToken;
                        ErrorCode usbError = transferContext.Wait(out int transferredCount);
                        if (usbError == ErrorCode.IoCancelled)
                            throw new TaskCanceledException();
                        if (usbError != ErrorCode.Ok)
                            throw new NetSyncException("Error reading from device");

                        return transferredCount;
                    }).ConfigureAwait(false);
                }

                Memory<byte> pipeBuffer = writer.GetMemory(bytesRead);
                ((Span<byte>)readBuffer).Slice(0, bytesRead).CopyTo(pipeBuffer.Span);
                writer.Advance(bytesRead);

                FlushResult result = await writer.FlushAsync().ConfigureAwait(false);

                if (result.IsCompleted)
                    break;
            }

            ArrayPool<byte>.Shared.Return(readBuffer);
            await writer.CompleteAsync().ConfigureAwait(false);
        }

        private async Task ProcessReadPipeAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;

                SequencePosition processedPosition = ReadPackets(buffer);

                reader.AdvanceTo(processedPosition, buffer.End);

                if (result.IsCompleted)
                    break;
            }

            await reader.CompleteAsync().ConfigureAwait(false);
        }

        private async Task RunWriterAsync(UsbDevice usbDevice, CancellationToken cancellationToken)
        {
            using UsbEndpointWriter usbWriter = usbDevice.OpenEndpointWriter(_writeEndpointId);

            while (!cancellationToken.IsCancellationRequested)
            {
                SendJob sendJob = await _sendQueue.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                ErrorCode errorCode = usbWriter.SubmitAsyncTransfer(sendJob.Buffer, 0, sendJob.Length, int.MaxValue, out UsbTransfer transferContext);
                if (errorCode != ErrorCode.Ok)
                    throw new NetSyncException("Failed to submit write transfer to USB device");

                using (transferContext)
                using (CancellationTokenRegistration cancelRegistration = cancellationToken.Register(() =>
                {
                    transferContext.Cancel();
                }))
                {
                    await Task.Run(() =>
                    {
                        ErrorCode usbError = transferContext.Wait(out int transferredCount);
                        if (usbError != ErrorCode.Ok)
                            throw new NetSyncException("Error reading from device");

                        return transferredCount;
                    }).ConfigureAwait(false);
                }

                sendJob.Dispose();
            }
        }
    }
}
