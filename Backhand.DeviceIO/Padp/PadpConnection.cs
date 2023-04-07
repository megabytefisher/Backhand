using Backhand.Common.Buffers;
using Backhand.DeviceIO.Slp;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Padp
{
    public class PadpConnection : IDlpTransport
    {
        public bool UseLongForm { get; set; } = false;

        private readonly SlpConnection _slpConnection;
        private readonly ArrayPool<byte> _arrayPool;

        private readonly byte _localSocketId;
        private readonly byte _remoteSocketId;
        private byte _currentTransactionId;

        private const byte PadpSlpPacketType = 0x02;
        private const int PadpMtu = sizeof(byte) * 1024;
        private const int PadpDefaultHeaderSize = sizeof(byte) * 4;
        private const int PadpLongHeaderSize = sizeof(byte) * 6;
        private static readonly TimeSpan AckTimeout = TimeSpan.FromSeconds(2);

        private enum PadpFragmentType : byte
        {
            Data = 0x01,
            Ack = 0x02,
            Tickle = 0x04,
        }

        [Flags]
        private enum PadpFragmentFlags : byte
        {
            None = 0b00000000,
            First = 0b10000000,
            Last = 0b01000000,
            MemoryError = 0b00100000,
            IsLongForm = 0b00010000,
        }

        public PadpConnection(SlpConnection slpConnection, byte localSocketId, byte remoteSocketId, byte initialTransactionId = 0xFF, ArrayPool<byte>? arrayPool = null)
        {
            _slpConnection = slpConnection;
            _localSocketId = localSocketId;
            _remoteSocketId = remoteSocketId;
            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
        }

        public async Task ExecuteTransactionAsync(ReadOnlySequence<byte> sendPayload, Action<ReadOnlySequence<byte>> handleReceivePayloadFunc, CancellationToken cancellationToken = default)
        {
            BumpTransactionId();
            Task receivePayloadTask = ReceivePayloadAsync(handleReceivePayloadFunc, cancellationToken);
            await SendPayloadAsync(sendPayload, cancellationToken).ConfigureAwait(false);
            await receivePayloadTask.ConfigureAwait(false);
        }

        private void BumpTransactionId()
        {
            _currentTransactionId++;
            if (_currentTransactionId is 0xFF or 0x00)
            {
                _currentTransactionId = 0x01;
            }
        }

        public async Task SendPayloadAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            // Send data in fragments
            for (int offset = 0; offset < buffer.Length; offset += PadpMtu)
            {
                uint txSize = (uint)Math.Min(buffer.Length - offset, PadpMtu);

                bool first = offset == 0;
                bool last = offset + txSize >= buffer.Length;
                PadpFragmentFlags flags =
                    (first ? PadpFragmentFlags.First : PadpFragmentFlags.None) |
                    (last ? PadpFragmentFlags.Last : PadpFragmentFlags.None);

                uint sizeOrOffset = first ? Convert.ToUInt32(buffer.Length) : Convert.ToUInt32(offset);

                Task ackWaitTask = WaitForAckAsync(sizeOrOffset, cancellationToken);
                EnqueueFragment(PadpFragmentType.Data, flags, sizeOrOffset, buffer.Slice(offset, txSize));
                await ackWaitTask;
            }
        }

        public async Task ReceivePayloadAsync(Action<ReadOnlySequence<byte>> handlePayloadFunc, CancellationToken cancellationToken)
        {
            TaskCompletionSource receiveTcs = new();
            SegmentBuffer<byte> payloadBuffer = new(_arrayPool);

            void OnSlpPacketReceived(object? sender, SlpTransmissionEventArgs e)
            {
                // Ignore any packets that aren't the correct PADP type or have incorrect sockets
                if (e.Packet.PacketType != PadpSlpPacketType || e.Packet.DestinationSocket != _localSocketId || e.Packet.SourceSocket != _remoteSocketId)
                {
                    return;
                }

                SequenceReader<byte> bufferReader = new SequenceReader<byte>(e.Packet.Data);

                // Parse header
                (PadpFragmentType packetType, PadpFragmentFlags packetFlags, uint packetSizeOrOffset) =
                    ReadFragmentHeader(ref bufferReader);

                // Ignore non-data packets
                if (packetType != PadpFragmentType.Data)
                {
                    return;
                }

                if (packetFlags.HasFlag(PadpFragmentFlags.First))
                {
                    // Our buffer should still be empty
                    if (payloadBuffer.Length != 0)
                    {
                        //throw new PadpException(
                        //    "Received data fragment marked as first, but already had data in buffer");
                    }
                }
                else
                {
                    // Our buffer length should match the sizeOrOffset
                    if (payloadBuffer.Length != packetSizeOrOffset)
                    {
                        //throw new PadpException("Received data fragment with unexpected sizeOrOffset");
                    }
                }

                // Copy current fragment data into buffer
                Span<byte> payloadBufferSegment = payloadBuffer.Append(Convert.ToInt32(bufferReader.Remaining));
                bufferReader.UnreadSpan.CopyTo(payloadBufferSegment);

                // Send ACK for the received fragment
                EnqueueAck(packetFlags, packetSizeOrOffset);

                // If not the last data fragment, continue listening.
                if (!packetFlags.HasFlag(PadpFragmentFlags.Last))
                {
                    return;
                }

                receiveTcs.TrySetResult();
            }

            _slpConnection.ReceivedPacket += OnSlpPacketReceived;
            try
            {
                await using (cancellationToken.Register(() => { receiveTcs.TrySetCanceled(); }))
                {
                    await receiveTcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                _slpConnection.ReceivedPacket -= OnSlpPacketReceived;
                payloadBuffer.Dispose();
            }
        }

        private void EnqueueAck(PadpFragmentFlags flags, uint sizeOrOffset)
        {
            EnqueueFragment(PadpFragmentType.Ack, flags, sizeOrOffset, ReadOnlySequence<byte>.Empty);
        }

        private async Task WaitForAckAsync(uint sizeOrOffset, CancellationToken cancellationToken)
        {
            TaskCompletionSource ackTcs = new();

            void OnSlpPacketReceived(object? sender, SlpTransmissionEventArgs e)
            {
                try
                {
                    // Ignore any packets that aren't the correct PADP type or have incorrect sockets
                    if (e.Packet.PacketType != PadpSlpPacketType || e.Packet.DestinationSocket != _localSocketId || e.Packet.SourceSocket != _remoteSocketId)
                    {
                        return;
                    }

                    SequenceReader<byte> bufferReader = new SequenceReader<byte>(e.Packet.Data);

                    // Parse header
                    (PadpFragmentType packetType, PadpFragmentFlags packetFlags, uint packetSizeOrOffset) =
                        ReadFragmentHeader(ref bufferReader);

                    if (e.Packet.TransactionId != _currentTransactionId || packetType != PadpFragmentType.Ack || sizeOrOffset != packetSizeOrOffset)
                    {
                        // Log unexpected packet
                    }

                    ackTcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    ackTcs.TrySetException(ex);
                }
            }

            _slpConnection.ReceivedPacket += OnSlpPacketReceived;

            using CancellationTokenSource timeoutCts = new(AckTimeout);
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            try
            {
                await using (linkedCts.Token.Register(() => { ackTcs.TrySetCanceled(); }))
                {
                    await ackTcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                _slpConnection.ReceivedPacket -= OnSlpPacketReceived;
            }
        }

        private void EnqueueFragment(PadpFragmentType type, PadpFragmentFlags flags, uint sizeOrOffset, ReadOnlySequence<byte> data)
        {
            // Figure out how much space we need for the fragment packet.
            int packetSize = Convert.ToInt32(data.Length + (UseLongForm ? PadpLongHeaderSize : PadpDefaultHeaderSize));

            // Get a buffer to hold the payload.
            byte[] buffer = _arrayPool.Rent(packetSize);

            try
            {
                SpanWriter<byte> bufferWriter = new SpanWriter<byte>(buffer);

                // Write header
                bufferWriter.Write((byte)type);
                bufferWriter.Write((byte)flags);

                if (UseLongForm)
                {
                    bufferWriter.WriteUInt32BigEndian(sizeOrOffset);
                }
                else
                {
                    bufferWriter.WriteUInt16BigEndian(Convert.ToUInt16(sizeOrOffset));
                }

                // Write data
                bufferWriter.WriteRange(data);

                // Build SLP packet
                SlpPacket slpPacket = new()
                {
                    DestinationSocket = _remoteSocketId,
                    SourceSocket = _localSocketId,
                    PacketType = PadpSlpPacketType,
                    TransactionId = _currentTransactionId,
                    Data = new ReadOnlySequence<byte>(buffer).Slice(0, packetSize)
                };

                // Enqueue packet
                _slpConnection.EnqueuePacket(slpPacket);
            }
            finally
            {
                // Return rented buffer
                _arrayPool.Return(buffer);
            }
        }

        private static (PadpFragmentType type, PadpFragmentFlags flags, uint sizeOrOffset) ReadFragmentHeader(ref SequenceReader<byte> bufferReader)
        {
            if (bufferReader.Remaining < 2)
            {
                // Throw
            }

            PadpFragmentType type = (PadpFragmentType)bufferReader.Read();
            PadpFragmentFlags flags = (PadpFragmentFlags)bufferReader.Read();

            uint sizeOrOffset;
            if (flags.HasFlag(PadpFragmentFlags.IsLongForm))
            {
                if (bufferReader.Remaining < sizeof(uint))
                {
                    // Throw
                }

                sizeOrOffset = bufferReader.ReadUInt32BigEndian();
            }
            else
            {
                if (bufferReader.Remaining < sizeof(ushort))
                {
                    // Throw
                }

                sizeOrOffset = bufferReader.ReadUInt16BigEndian();
            }

            return (type, flags, sizeOrOffset);
        }
    }
}
