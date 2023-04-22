using System.Buffers;
using System.IO.Pipelines;
using Backhand.Usb.Internal;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Backhand.Usb
{
    public sealed class UsbDevicePipe : IDuplexPipe, IDisposable, IAsyncDisposable
    {
        public UsbDeviceConnection Connection { get; }
        public PipeReader Input => _fromUsb.Reader;
        public PipeWriter Output => _toUsb.Writer;

        // A buffer too large can crash the device
        public int WriteBufferSize { get; init; } = 64;
        public int ReadBufferSize { get; init; } = 64;

        private readonly Pipe _fromUsb = new();
        private readonly Pipe _toUsb = new();

        private readonly UsbDeviceConnection _deviceConnection;
        private readonly ReadEndpointID _readEndpoint;
        private readonly WriteEndpointID _writeEndpoint;

        private readonly bool _disposeConnection;
        private readonly CancellationTokenRegistration? _cancellationTokenRegistration;
        
        private bool _disposed;

        public UsbDevicePipe(UsbDeviceConnection deviceConnection, ReadEndpointID readEndpoint, WriteEndpointID writeEndpoint, bool disposeConnection = false, CancellationToken? cancellationToken = null)
        {
            Connection = deviceConnection;
            _deviceConnection = deviceConnection;
            _readEndpoint = readEndpoint;
            _writeEndpoint = writeEndpoint;
            _disposeConnection = disposeConnection;
            
            _cancellationTokenRegistration = cancellationToken?.Register(() =>
            {
                _fromUsb.Reader.Complete(new TaskCanceledException());
                _fromUsb.Writer.Complete(new TaskCanceledException());
                _toUsb.Writer.Complete(new TaskCanceledException());
                _toUsb.Reader.Complete(new TaskCanceledException());
            });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cancellationTokenRegistration?.Dispose();
            _fromUsb.Reader.Complete();
            _fromUsb.Writer.Complete();
            _toUsb.Reader.Complete();
            _toUsb.Writer.Complete();

            if (_disposeConnection) _deviceConnection.Dispose();
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            if (_cancellationTokenRegistration != null)
            {
                await _cancellationTokenRegistration.Value.DisposeAsync().ConfigureAwait(false);
            }
            
            await _fromUsb.Reader.CompleteAsync().ConfigureAwait(false);
            await _fromUsb.Writer.CompleteAsync().ConfigureAwait(false);
            await _toUsb.Reader.CompleteAsync().ConfigureAwait(false);
            await _toUsb.Writer.CompleteAsync().ConfigureAwait(false);

            if (_disposeConnection) _deviceConnection.Dispose();
        }

        public async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource innerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            using UsbEndpointReader usbReader = _deviceConnection.UsbDevice.OpenEndpointReader(_readEndpoint);
            using UsbEndpointWriter usbWriter = _deviceConnection.UsbDevice.OpenEndpointWriter(_writeEndpoint);
            
            Task writeTask = WriteToUsbAsync(_toUsb.Reader, usbWriter, innerCts.Token);
            Task readTask = ReadToPipeAsync(usbReader, _fromUsb.Writer, innerCts.Token);

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

                // Read some bytes from the incoming pipe
                ReadResult readResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = readResult.Buffer;

                // Figure out how many bytes we can send
                int sendLength = Convert.ToInt32(Math.Min(WriteBufferSize, buffer.Length));
                
                // Copy the bytes to the write buffer
                buffer.Slice(0, sendLength).CopyTo(writeBuffer);
                
                // Advance the pipe reader
                pipeReader.AdvanceTo(buffer.GetPosition(sendLength));

                // Submit the USB write transfer
                ErrorCode submitErrorCode = usbWriter.SubmitAsyncTransfer(
                    writeBuffer,
                    0,
                    sendLength,
                    int.MaxValue,
                    out UsbTransfer usbTransfer
                );
                using UsbTransfer usbTransferDispose = usbTransfer;
                
                if (submitErrorCode != ErrorCode.Ok)
                {
                    throw new UsbException("Failed to submit write transfer");
                }

                // Wait for the transfer to complete
                (ErrorCode errorCode, int bytesWritten) =
                    await usbTransfer.WaitAsync(cancellationToken).ConfigureAwait(false);
                
                if (errorCode != ErrorCode.Ok)
                {
                    throw new UsbException("Failed to wait for write transfer");
                }

                if (bytesWritten != sendLength)
                {
                    throw new UsbException("Failed to write all bytes");
                }

                if (readResult.IsCompleted) break;
            }
        }

        private async Task ReadToPipeAsync(UsbEndpointReader usbReader, PipeWriter pipeWriter, CancellationToken cancellationToken)
        {
            byte[] readBuffer = new byte[ReadBufferSize];

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Submit the USB read transfer
                ErrorCode submitErrorCode = usbReader.SubmitAsyncTransfer(
                    readBuffer,
                    0,
                    ReadBufferSize, 
                    int.MaxValue,
                    out UsbTransfer usbTransfer
                );
                using UsbTransfer? usbTransferDispose = usbTransfer;
                
                if (submitErrorCode != ErrorCode.Ok)
                {
                    throw new UsbException("Failed to submit read transfer");
                }

                // Wait for the transfer to complete
                (ErrorCode errorCode, int bytesRead) =
                    await usbTransfer.WaitAsync(cancellationToken).ConfigureAwait(false);
                
                if (errorCode != ErrorCode.Ok)
                {
                    throw new UsbException("Failed to wait for read transfer");
                }

                // Write the bytes to the pipe
                Memory<byte> pipeBuffer = pipeWriter.GetMemory(bytesRead);
                readBuffer[..bytesRead].CopyTo(pipeBuffer.Span);
                pipeWriter.Advance(bytesRead);

                FlushResult flushResult = await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                if (flushResult.IsCompleted) break;
            }
        }
    }
}