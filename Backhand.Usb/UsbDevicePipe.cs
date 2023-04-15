using System.Buffers;
using System.IO.Pipelines;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Backhand.Usb
{
    public class UsbDevicePipe : IDisposable
    {
        public PipeReader Reader => _readPipe.Reader;
        public PipeWriter Writer => _writePipe.Writer;

        private readonly Pipe _readPipe = new();
        private readonly Pipe _writePipe = new();

        private readonly UsbDevice _device;
        private readonly ReadEndpointID _readEndpoint;
        private readonly WriteEndpointID _writeEndpoint;

        // A buffer too large can crash the device
        private const int WriteBufferSize = 64;
        private const int ReadBufferSize = 64;

        public static async Task<UsbDevicePipe> RunPipe(UsbDevice device, ReadEndpointID readEndpoint, WriteEndpointID writeEndpoint, CancellationToken cancellationToken = default)
        {
            UsbDevicePipe pipe = new(device, readEndpoint, writeEndpoint);
            try
            {
                await pipe.RunIOAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
            }
        }

        public UsbDevicePipe(UsbDevice device, ReadEndpointID readEndpoint, WriteEndpointID writeEndpoint)
        {
            _device = device;
            _readEndpoint = readEndpoint;
            _writeEndpoint = writeEndpoint;
        }

        public async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource innerCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCts.Token);

            UsbEndpointReader usbReader = _device.OpenEndpointReader(_readEndpoint);
            UsbEndpointWriter usbWriter = _device.OpenEndpointWriter(_writeEndpoint);

            Task writeTask = WriteToUsbAsync(_writePipe.Reader, usbWriter, linkedCts.Token);
            Task readTask = ReadToPipeAsync(usbReader, _readPipe.Writer, linkedCts.Token);

            await Task.WhenAny(writeTask, readTask).ConfigureAwait(false);
            innerCts.Cancel();
            await Task.whenAll(writeTask, readTask).ConfigureAwait(false);
        }

        private async Task WriteToUsbAsync(PipeReader pipeReader, UsbEndpointWriter usbWriter, CancellationToken cancellationToken)
        {
            byte[] writeBuffer = new byte[WriteBufferSize];

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ReadResult readResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = readResult.Buffer;

                int sendLength = Convert.ToInt32(Math.Min(WriteBufferSize, buffer.Length));
                buffer.Slice(0, sendLength).CopyTo(writeBuffer);
                pipeReader.AdvanceTo(buffer.Slice(sendLength).Start, buffer.Slice(sendLength).Start);

                ErrorCode submitErrorCode = usbWriter.SubmitAsyncTransfer(writeBuffer, 0, sendLength, int.MaxValue, out UsbTransfer usbTransfer);
                if (submitErrorCode != ErrorCode.Ok)
                {
                    throw new UsbException("Failed to submit write transfer");
                }

                using (usbTransfer)
                await using (cancellationToken.Register(() => usbTransfer.Cancel()))
                {
                    await Task.Run(() =>
                    {
                        ErrorCode transferErrorCode = usbTransfer.Wait();
                        if (transferErrorCode == ErrorCode.IoCancelled)
                        {
                            throw new OperationCanceledException("Write transfer cancelled");
                        }
                        if (transferErrorCode != ErrorCode.Ok)
                        {
                            throw new UsbException("Failed to wait for write transfer");
                        }
                    }, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task ReadToPipeAsync(UsbEndpointReader usbReader, PipeWriter pipeWriter, CancellationToken cancellationToken)
        {
            byte[] readBuffer = new byte[ReadBufferSize];

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ErrorCode submitErrorCode = usbReader.SubmitAsyncTransfer(readBuffer, 0, ReadBufferSize, int.MaxValue, out UsbTransfer usbTransfer);
                if (submitErrorCode != ErrorCode.Ok)
                {
                    throw new UsbException("Failed to submit read transfer");
                }

                int bytesRead = 0;

                using (usbTransfer)
                await using (cancellationToken.Register(() => usbTransfer.Cancel()))
                {
                    await Task.Run(() =>
                    {
                        ErrorCode transferErrorCode = usbTransfer.Wait(out bytesRead);
                        if (transferErrorCode == ErrorCode.IoCancelled)
                        {
                            throw new OperationCanceledException("Read transfer cancelled");
                        }
                        if (transferErrorCode != ErrorCode.Ok)
                        {
                            throw new UsbException("Failed to wait for read transfer");
                        }
                    }, cancellationToken).ConfigureAwait(false);
                }

                Memory<byte> pipeBuffer = pipeWriter.GetMemory(bytesRead);
                readBuffer[..bytesRead].CopyTo(pipeBuffer.Span);
                pipeWriter.Advance(bytesRead);

                FlushResult flushResult = await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                if (flushResult.IsCompleted)
                {
                    break;
                }
            }
        }
    }
}