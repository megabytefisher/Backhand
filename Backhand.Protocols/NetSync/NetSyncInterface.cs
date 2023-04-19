using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Backhand.Common.Buffers;
using Backhand.Protocols.NetSync.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backhand.Protocols.NetSync
{
    public class NetSyncInterface : IDisposable
    {
        public event EventHandler<NetSyncTransmissionEventArgs>? PacketReceived;

        private readonly IDuplexPipe _pipe;
        private readonly ArrayPool<byte> _arrayPool;
        private readonly BufferBlock<SendJob> _sendQueue;

        private readonly ILogger _logger;

        // Constants
        private const int NetSyncHeaderSize = sizeof(byte) * 6;

        public NetSyncInterface(IDuplexPipe pipe, ArrayPool<byte>? arrayPool = null, ILogger? logger = null)
        {
            _pipe = pipe;
            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
            _sendQueue = new BufferBlock<SendJob>();
            _logger = logger ?? NullLogger.Instance;
        }

        public void Dispose()
        {
            _sendQueue.Complete();

            if (_sendQueue.TryReceiveAll(out IList<SendJob>? sendJob))
            {
                foreach (SendJob job in sendJob)
                {
                    job.Dispose();
                }
            }
        }

        public async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource innerCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCts.Token);

            Task readTask = RunReadLoopAsync(linkedCts.Token);
            Task writeTask = RunWriteLoopAsync(linkedCts.Token);

            await Task.WhenAny(readTask, writeTask).ConfigureAwait(false);

            innerCts.Cancel();
            await Task.WhenAll(readTask, writeTask).ConfigureAwait(false);
        }

        public void EnqueuePacket(NetSyncPacket packet)
        {
            _logger.EnqueueingPacket(packet);
            SendJob sendJob = CreateSendJob(packet);
            if (!_sendQueue.Post(sendJob))
            {
                sendJob.Dispose();
            }
        }

        private SendJob CreateSendJob(NetSyncPacket packet)
        {
            int length = Convert.ToInt32(NetSyncHeaderSize + packet.Data.Length);
            SendJob job = new(_arrayPool, length);
            WritePacket(packet, job.Buffer);
            return job;
        }

        private async Task RunReadLoopAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ReadResult readResult = await _pipe.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                _logger.ReadBytes(readResult.Buffer);

                if (readResult.IsCompleted)
                    break;

                SequencePosition nextPosition = ReadPackets(readResult.Buffer);
                _pipe.Input.AdvanceTo(nextPosition, readResult.Buffer.End);
            }
        }

        private async Task RunWriteLoopAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using SendJob job = await _sendQueue.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                _logger.WritingBytes(job.Buffer);
                job.Buffer.CopyTo(_pipe.Output.GetSpan(job.Buffer.Length));
                _pipe.Output.Advance(job.Buffer.Length);
                FlushResult flushResult = await _pipe.Output.FlushAsync(cancellationToken).ConfigureAwait(false);

                if (flushResult.IsCompleted)
                    break;
            }
        }

        private SequencePosition ReadPackets(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            
            while (bufferReader.Remaining >= NetSyncHeaderSize)
            {
                SequencePosition packetStart = bufferReader.Position;

                byte dataType = bufferReader.Read();
                byte transactionId = bufferReader.Read();
                uint dataLength = bufferReader.ReadUInt32BigEndian();

                if (dataType != 0x01)
                {
                    throw new NetSyncException($"Invalid data type: {dataType}");
                }

                if (bufferReader.Remaining < dataLength)
                {
                    // Not enough data in our buffer. Need to try again from the beginning next time.
                    return packetStart;
                }

                ReadOnlySequence<byte> packetData = bufferReader.Sequence.Slice(bufferReader.Position, dataLength);
                bufferReader.Advance(dataLength);

                NetSyncPacket packet = new(transactionId, packetData);
                _logger.ReceivedPacket(packet);
                PacketReceived?.Invoke(this, new NetSyncTransmissionEventArgs(packet));
            }

            return bufferReader.Position;
        }

        private static void WritePacket(NetSyncPacket packet, Span<byte> buffer)
        {
            SpanWriter<byte> bufferWriter = new(buffer);

            bufferWriter.Write((byte)1); // Data type
            bufferWriter.Write(packet.TransactionId);
            bufferWriter.WriteUInt32BigEndian((uint)packet.Data.Length);
            packet.Data.CopyTo(bufferWriter.RemainingSpan);
        }

        private class SendJob : IDisposable
        {
            public Span<byte> Buffer => new(_array, 0, _length);

            private readonly ArrayPool<byte> _arrayPool;
            private readonly int _length;
            private readonly byte[] _array;

            private bool _disposed;

            public SendJob(ArrayPool<byte> arrayPool, int length)
            {
                _arrayPool = arrayPool;
                _length = length;
                _array = arrayPool.Rent(length);
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _arrayPool.Return(_array);
                _disposed = true;
            }
        }
    }
}
