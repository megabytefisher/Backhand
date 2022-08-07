using Backhand.DeviceIO.NetSync;
using Backhand.Utility.Buffers;
using MadWizard.WinUSBNet;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Backhand.DeviceIO.Usb.Windows
{
    public class WindowsUsbNetSyncDevice : NetSyncDevice, IDisposable
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
            public ExtConnectionPortInfo[] Ports { get; set; }
        }

        private class ExtConnectionPortInfo
        {
            public string Type { get; set; }
            public byte PortNumber { get; set; }
            public byte InEndpoint { get; set; }
            public byte OutEndpoint { get; set; }
        }

        private USBDevice _usbDevice;

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

        public WindowsUsbNetSyncDevice(USBDeviceInfo usbDeviceInfo)
        {
            _usbDevice = new USBDevice(usbDeviceInfo);
            _sendQueue = new BufferBlock<SendJob>();
        }

        public void Dispose()
        {
            _usbDevice.Dispose();
            
            if (_sendQueue.TryReceiveAll(out IList<SendJob>? sendJobs))
            {
                foreach (SendJob sendJob in sendJobs)
                {
                    sendJob.Dispose();
                }
            }

            _sendQueue.Complete();
        }

        public async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            DoHardwareHandshake();

            using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationToken.None))
            {
                Task readerTask = RunReaderAsync(linkedCts.Token);
                Task writerTask = RunWriterAsync(linkedCts.Token);

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

        private void DoHardwareHandshake()
        {
            // TODO: convert Control methods to async..
            byte[] result = new byte[20];
            int len = _usbDevice.ControlIn(UsbControlTransferRequestType, 0x04, 0x00, 0x00, result);

            GetExtConnectionInfoResponse response = ReadGetExtConnectionInfoResponse(new ReadOnlySequence<byte>(result, 0, len));

            ExtConnectionPortInfo? syncPort = response.Ports.FirstOrDefault(p => p.Type == "cnys");
            if (syncPort == null)
                throw new NetSyncException("Couldn't find correct USB device port");

            _inEndpoint = (byte)(syncPort.InEndpoint | 0b10000000);
            _outEndpoint = syncPort.OutEndpoint;
        }

        private async Task RunReaderAsync(CancellationToken cancellationToken)
        {
            Pipe readerPipe = new Pipe();
            await Task.WhenAll(
                FillReadPipeAsync(readerPipe.Writer, cancellationToken),
                ProcessReadPipeAsync(readerPipe.Reader, cancellationToken)).ConfigureAwait(false);
        }

        private async Task FillReadPipeAsync(PipeWriter writer, CancellationToken cancellationToken)
        {
            const int minimumBufferSize = 256;

            USBPipe usbPipe = _usbDevice.Interfaces.First().Pipes[_inEndpoint.Value];

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    byte[] readBuffer = ArrayPool<byte>.Shared.Rent(minimumBufferSize);

                    TaskCompletionSource<int> readTcs = new TaskCompletionSource<int>();
                    usbPipe.BeginRead(readBuffer, 0, readBuffer.Length, (result) =>
                    {
                        try
                        {
                            readTcs.TrySetResult(usbPipe.EndRead(result));
                        }
                        catch (Exception ex)
                        {
                            readTcs.TrySetException(ex);
                        }
                    }, null);
                    int bytesRead = await readTcs.Task.ConfigureAwait(false);

                    Memory<byte> pipeBuffer = writer.GetMemory(bytesRead);
                    ((Span<byte>)readBuffer).Slice(0, bytesRead).CopyTo(pipeBuffer.Span);
                    writer.Advance(bytesRead);

                    FlushResult result = await writer.FlushAsync().ConfigureAwait(false);

                    if (result.IsCompleted)
                        break;
                }
                catch (Exception ex)
                {
                    // TODO : log
                    break;
                }
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

        private async Task RunWriterAsync(CancellationToken cancellationToken)
        {
            USBPipe usbPipe = _usbDevice.Interfaces.First().Pipes[_outEndpoint.Value];

            while (!cancellationToken.IsCancellationRequested)
            {
                SendJob sendJob = await _sendQueue.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                TaskCompletionSource writeTcs = new TaskCompletionSource();
                usbPipe.BeginWrite(sendJob.Buffer, 0, sendJob.Length, (result) =>
                {
                    usbPipe.EndWrite(result);
                    writeTcs.TrySetResult();
                }, null);
                await writeTcs.Task.ConfigureAwait(false);

                sendJob.Dispose();
            }
        }

        /*private async Task RunWriterAsync(CancellationToken cancellationToken)
        {
            PipeWriter serialPortWriter = PipeWriter.Create(_serialPort.BaseStream);

            while (!cancellationToken.IsCancellationRequested)
            {
                SlpSendJob sendJob = await _sendQueue.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                Memory<byte> sendBuffer = serialPortWriter.GetMemory(sendJob.Length);
                sendJob.Buffer.CopyTo(sendBuffer);

                serialPortWriter.Advance(sendJob.Length);
                await serialPortWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Dispose sendJob to return allocated array to pool
                sendJob.Dispose();
            }
        }*/

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
