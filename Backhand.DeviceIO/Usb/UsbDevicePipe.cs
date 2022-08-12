using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Usb
{
    public class UsbDevicePipe : IDuplexPipe
    {
        private UsbDevice _usbDevice;
        private ReadEndpointID _readEndpointId;
        private WriteEndpointID _writeEndpointId;

        private PipeReader? _inputReader;
        private PipeWriter? _outputWriter;

        public UsbDevicePipe(UsbDevice usbDevice, ReadEndpointID readEndpointId, WriteEndpointID writeEndpointId)
        {
            _usbDevice = usbDevice;
            _readEndpointId = readEndpointId;
            _writeEndpointId = writeEndpointId;
        }

        // TODO : add exceptions if they're null (RunReaderAsync hasn't been called)
        public PipeReader Input => _inputReader!;
        public PipeWriter Output => _outputWriter!;

        public async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource abortCts = new CancellationTokenSource();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, abortCts.Token);

            Task readerTask = RunReaderAsync(linkedCts.Token);
            Task writerTask = RunWriterAsync(linkedCts.Token);

            try
            {
                await Task.WhenAny(readerTask, writerTask);
            }
            catch
            {
            }

            abortCts.Cancel();
            await Task.WhenAll(readerTask, writerTask);
        }

        private async Task RunReaderAsync(CancellationToken cancellationToken)
        {
            Pipe inputPipe = new Pipe();
            _inputReader = inputPipe.Reader;

            using UsbEndpointReader usbReader = _usbDevice.OpenEndpointReader(_readEndpointId);

            await FillInputPipeAsync(inputPipe.Writer, usbReader, cancellationToken);
        }

        private async Task RunWriterAsync(CancellationToken cancellationToken)
        {
            Pipe outputPipe = new Pipe();
            _outputWriter = outputPipe.Writer;

            using UsbEndpointWriter usbWriter = _usbDevice.OpenEndpointWriter(_writeEndpointId);

            await ProcessOutputPipeAsync(outputPipe.Reader, usbWriter, cancellationToken);
        }

        private static async Task FillInputPipeAsync(PipeWriter pipeWriter, UsbEndpointReader usbReader, CancellationToken cancellationToken)
        {
            // A buffer too big can crash the device.
            const int readBufferSize = 64;
            byte[] readBuffer = new byte[readBufferSize];
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ErrorCode errorCode = usbReader.SubmitAsyncTransfer(readBuffer, 0, readBufferSize, int.MaxValue, out UsbTransfer transferContext);
                    if (errorCode != ErrorCode.Ok)
                        throw new UsbDeviceException("Failed to submit read transfer");

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
                            if (errorCode == ErrorCode.IoCancelled)
                                throw new TaskCanceledException();
                            if (errorCode != ErrorCode.Ok)
                                throw new UsbDeviceException("Failed to complete read transfer");
                        }).ConfigureAwait(false);
                    }

                    Memory<byte> pipeBuffer = pipeWriter.GetMemory(bytesRead);
                    ((Span<byte>)readBuffer).Slice(0, bytesRead).CopyTo(pipeBuffer.Span);
                    pipeWriter.Advance(bytesRead);

                    FlushResult result = await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                    if (result.IsCompleted)
                        break;
                }
            }
            finally
            {
                await pipeWriter.CompleteAsync();
            }
        }

        private static async Task ProcessOutputPipeAsync(PipeReader pipeReader, UsbEndpointWriter usbWriter, CancellationToken cancellationToken)
        {
            const int writeBufferSize = 64;
            byte[] writeBuffer = new byte[writeBufferSize];

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ReadResult readResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = readResult.Buffer;

                    int sendLength = Convert.ToInt32(Math.Min(writeBufferSize, buffer.Length));
                    buffer.Slice(0, sendLength).CopyTo(writeBuffer);
                    pipeReader.AdvanceTo(buffer.Slice(sendLength).Start, buffer.Slice(sendLength).Start);

                    ErrorCode errorCode = usbWriter.SubmitAsyncTransfer(writeBuffer, 0, sendLength, int.MaxValue, out UsbTransfer transferContext);
                    if (errorCode != ErrorCode.Ok)
                        throw new UsbDeviceException("Failed to submit write transfer");

                    using (transferContext)
                    using (cancellationToken.Register(() =>
                    {
                        transferContext.Cancel();
                    }))
                    {
                        await Task.Run(() =>
                        {
                            ErrorCode errorCode = transferContext.Wait(out int transferredCount);
                            if (errorCode == ErrorCode.IoCancelled)
                                throw new TaskCanceledException();
                            if (errorCode != ErrorCode.Ok)
                                throw new UsbDeviceException("Failed to complete write transfer");
                            if (transferredCount != sendLength)
                                throw new UsbDeviceException("Failed to write all bytes");
                        });
                    }
                }
            }
            finally
            {
                await pipeReader.CompleteAsync();
            }
        }
    }
}
