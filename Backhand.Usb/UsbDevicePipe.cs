using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Backhand.Usb
{
    public class UsbDevicePipe : IDuplexPipe, IDisposable
    {
        public PipeReader Input => _fromUsb.Reader;
        public PipeWriter Output => _toUsb.Writer;

        private readonly Pipe _fromUsb = new();
        private readonly Pipe _toUsb = new();

        private readonly UsbDeviceConnection _deviceConnection;
        private readonly ReadEndpointID _readEndpoint;
        private readonly WriteEndpointID _writeEndpoint;

        // A buffer too large can crash the device
        private const int WriteBufferSize = 64;
        private const int ReadBufferSize = 64;

        public UsbDevicePipe(UsbDeviceConnection deviceConnection, ReadEndpointID readEndpoint, WriteEndpointID writeEndpoint)
        {
            _deviceConnection = deviceConnection;
            _readEndpoint = readEndpoint;
            _writeEndpoint = writeEndpoint;
        }

        public void Dispose()
        {
            _fromUsb.Reader.Complete();
            _fromUsb.Writer.Complete();
            _toUsb.Reader.Complete();
            _toUsb.Writer.Complete();
        }

        public void Complete()
        {
            _fromUsb.Reader.Complete();
            _fromUsb.Writer.Complete();
            _toUsb.Reader.Complete();
            _toUsb.Writer.Complete();
        }

        public async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource innerCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCts.Token);

            using UsbEndpointReader usbReader = _deviceConnection.UsbDevice.OpenEndpointReader(_readEndpoint);
            using UsbEndpointWriter usbWriter = _deviceConnection.UsbDevice.OpenEndpointWriter(_writeEndpoint);
            
            Task writeTask = WriteToUsbAsync(_toUsb.Reader, usbWriter, linkedCts.Token);
            Task readTask = ReadToPipeAsync(usbReader, _fromUsb.Writer, linkedCts.Token);

            await Task.WhenAny(writeTask, readTask).ConfigureAwait(false);
            innerCts.Cancel();
            await Task.WhenAll(writeTask, readTask).ConfigureAwait(false);
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
                        ErrorCode transferErrorCode = usbTransfer.Wait(out int bytesWritten);
                        if (bytesWritten != sendLength)
                        {
                            throw new UsbException("Failed to write all bytes");
                        }
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