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

        private class GetExtConnectionInfoResponse
        {
            public byte PortCount { get; set; }
            public bool HasDifferentEndpoints { get; set; }
            public ExtConnectionPortInfo[] Ports { get; set; } = Array.Empty<ExtConnectionPortInfo>();
        }

        private class ExtConnectionPortInfo
        {
            public string Type { get; set; } = "";
            public byte PortNumber { get; set; }
            public byte InEndpoint { get; set; }
            public byte OutEndpoint { get; set; }
        }

        private UsbRegistry _usbRegistry;

        private byte? _inEndpoint;
        private byte? _outEndpoint;

        private BufferBlock<SendJob> _sendQueue;

        // Device-to-host, Vendor, Endpoint
        private const byte UsbControlTransferRequestType = 0xC2;

        private const int GetExtConnectionInfoResponseLength = 20;
        private const int ExtConnectionInEndpointBitmask = 0b11110000;
        private const int ExtConnectionInEndpointShift = 4;
        private const int ExtConnectionOutEndpointBitmask = 0b00001111;
        private const int ExtConnectionOutEndpointShift = 0;

        public UsbNetSyncDevice(UsbRegistry usbRegistry)
        {
            _usbRegistry = usbRegistry;
            _sendQueue = new BufferBlock<SendJob>();
        }

        public void Dispose()
        {
            _sendQueue.Complete();
        }

        public async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            bool success = _usbRegistry.Open(out UsbDevice usbDevice);
            if (!success)
                throw new NetSyncException("Failed to open USB device");

            try
            {
                DoHardwareHandshake(usbDevice);

                using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationToken.None))
                {
                    Task readerTask = RunReaderAsync(usbDevice, linkedCts.Token);
                    Task writerTask = RunWriterAsync(usbDevice, linkedCts.Token);

                    await Task.WhenAll(readerTask, writerTask).ConfigureAwait(false);
                }
            }
            finally
            {
                usbDevice.Close();
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

        private void DoHardwareHandshake(UsbDevice usbDevice)
        {
            // TODO: convert Control methods to async..
            byte[] result = new byte[20];
            UsbSetupPacket setupPacket = new UsbSetupPacket(UsbControlTransferRequestType, 0x04, 0, 0, 20);
            bool success = usbDevice.ControlTransfer(ref setupPacket, result, 20, out int len);
            if (len != 20)
                throw new NetSyncException("Didn't get expected length from USB control transfer");

            GetExtConnectionInfoResponse response = ReadGetExtConnectionInfoResponse(new ReadOnlySequence<byte>(result, 0, len));

            ExtConnectionPortInfo? syncPort = response.Ports.FirstOrDefault(p => p.Type == "cnys");
            if (syncPort == null)
                throw new NetSyncException("Couldn't find correct USB device port");

            _inEndpoint = (byte)(syncPort.InEndpoint | 0b10000000);
            _outEndpoint = syncPort.OutEndpoint;
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
            const int minimumBufferSize = 256;
            byte[] readBuffer = ArrayPool<byte>.Shared.Rent(minimumBufferSize);

            using UsbEndpointReader usbReader = usbDevice.OpenEndpointReader((ReadEndpointID)_inEndpoint!);

            while (!cancellationToken.IsCancellationRequested)
            {
                ErrorCode errorCode = usbReader.SubmitAsyncTransfer(readBuffer, 0, readBuffer.Length, int.MaxValue, out UsbTransfer transferContext);
                if (errorCode != ErrorCode.Ok)
                    throw new NetSyncException("Failed to submit read transfer on USB device");

                int bytesRead;
                using (CancellationTokenRegistration cancelRegistration = cancellationToken.Register(() =>
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
            using UsbEndpointWriter usbWriter = usbDevice.OpenEndpointWriter((WriteEndpointID)_outEndpoint!);

            while (!cancellationToken.IsCancellationRequested)
            {
                SendJob sendJob = await _sendQueue.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                ErrorCode errorCode = usbWriter.SubmitAsyncTransfer(sendJob.Buffer, 0, sendJob.Length, int.MaxValue, out UsbTransfer transferContext);
                if (errorCode != ErrorCode.Ok)
                    throw new NetSyncException("Failed to submit write transfer to USB device");

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

        private GetExtConnectionInfoResponse ReadGetExtConnectionInfoResponse(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);

            GetExtConnectionInfoResponse result = new GetExtConnectionInfoResponse();
            result.PortCount = bufferReader.Read();
            result.HasDifferentEndpoints = bufferReader.Read() != 0;
            bufferReader.Advance(2); // padding
            result.Ports = new ExtConnectionPortInfo[result.PortCount];
            for (int i = 0; i < result.PortCount; i++)
            {
                result.Ports[i] = ReadExtConnectionPortInfo(ref bufferReader);
            }

            return result;
        }

        private ExtConnectionPortInfo ReadExtConnectionPortInfo(ref SequenceReader<byte> bufferReader)
        {
            ExtConnectionPortInfo result = new ExtConnectionPortInfo();
            result.Type = Encoding.ASCII.GetString(bufferReader.Sequence.Slice(bufferReader.Position, 4));
            bufferReader.Advance(4);

            result.PortNumber = bufferReader.Read();

            byte endpoints = bufferReader.Read();
            result.InEndpoint = (byte)((endpoints & ExtConnectionInEndpointBitmask) >> ExtConnectionInEndpointShift);
            result.OutEndpoint = (byte)((endpoints & ExtConnectionOutEndpointBitmask) >> ExtConnectionOutEndpointShift);

            bufferReader.Advance(2); // padding

            return result;
        }
    }
}
