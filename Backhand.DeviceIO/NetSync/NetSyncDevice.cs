using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Backhand.DeviceIO.NetSync
{
    public class NetSyncDevice
    {
        private class NetSyncSendJob : IDisposable
        {
            public int Length { get; }
            public byte[] Buffer { get; }

            public NetSyncSendJob(int length)
            {
                Length = length;
                Buffer = ArrayPool<byte>.Shared.Rent(length);
            }

            public void Dispose()
            {
                ArrayPool<byte>.Shared.Return(Buffer);
            }
        }

        public event EventHandler<NetSyncPacketTransmittedEventArgs>? ReceivedPacket;

        private readonly IDuplexPipe _basePipe;
        private readonly BufferBlock<NetSyncSendJob> _sendQueue;

        private const int NetSyncHeaderLength = sizeof(byte) * 6;

        public NetSyncDevice(IDuplexPipe basePipe)
        {
            _basePipe = basePipe;
            _sendQueue = new BufferBlock<NetSyncSendJob>();
        }

        public void SendPacket(NetSyncPacket packet)
        {
            // Get packet length and allocate buffer
            int packetLength = (int)GetPacketLength(packet);
            NetSyncSendJob sendJob = new(packetLength);

            // Write packet
            WritePacket(packet, ((Span<byte>)sendJob.Buffer).Slice(0, packetLength));

            // Enqueue send job
            if (!_sendQueue.Post(sendJob))
            {
                throw new NetSyncException("Failed to post packet to send queue");
            }
        }

        public async Task RunIoAsync(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource abortCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, abortCts.Token);

            Task readerTask = RunReaderAsync(cancellationToken);
            Task writerTask = RunWriterAsync(cancellationToken);

            try
            {
                await Task.WhenAny(readerTask, writerTask).ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }

            abortCts.Cancel();
            
            await Task.WhenAll(readerTask, writerTask).ConfigureAwait(false);
        }

        private async Task RunReaderAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult readResult = await _basePipe.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = readResult.Buffer;

                SequencePosition processedPosition = ReadPackets(buffer);

                _basePipe.Input.AdvanceTo(processedPosition);

                if (readResult.IsCompleted)
                    break;
            }
        }

        private async Task RunWriterAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                NetSyncSendJob sendJob = await _sendQueue.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                Memory<byte> pipeBuffer = _basePipe.Output.GetMemory(sendJob.Length);
                ((Span<byte>)sendJob.Buffer).Slice(0, sendJob.Length).CopyTo(pipeBuffer.Span);
                _basePipe.Output.Advance(sendJob.Length);

                FlushResult flushResult = await _basePipe.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
                if (flushResult.IsCompleted)
                    break;
            }
        }

        private SequencePosition ReadPackets(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            
            while (bufferReader.Remaining >= NetSyncHeaderLength)
            {
                SequencePosition packetStart = bufferReader.Position;

                byte dataType = bufferReader.Read();
                byte transactionId = bufferReader.Read();
                uint payloadLength = bufferReader.ReadUInt32BigEndian();

                if (dataType != 0x01)
                    throw new NetSyncException("Unexpected NetSync packet data type");

                if (bufferReader.Remaining < payloadLength)
                {
                    // Not enough data
                    return packetStart;
                }

                ReadOnlySequence<byte> payloadBuffer = buffer.Slice(bufferReader.Position, payloadLength);
                bufferReader.Advance(payloadLength);

                ReceivedPacket?.Invoke(this, new NetSyncPacketTransmittedEventArgs(new NetSyncPacket(transactionId, payloadBuffer)));
            }

            return bufferReader.Position;
        }

        private static uint GetPacketLength(NetSyncPacket packet)
        {
            return Convert.ToUInt32(NetSyncHeaderLength + packet.Data.Length);
        }

        private static void WritePacket(NetSyncPacket packet, Span<byte> buffer)
        {
            int offset = 0;
            
            buffer[offset] = 0x01; // data type
            offset += sizeof(byte);
            
            buffer[offset] = packet.TransactionId;
            offset += sizeof(byte);
            
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, sizeof(uint)), (uint)packet.Data.Length);
            offset += sizeof(uint);
            
            packet.Data.CopyTo(buffer.Slice(offset));
        }
    }
}
