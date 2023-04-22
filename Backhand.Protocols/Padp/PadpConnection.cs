using Backhand.Common.Buffers;
using Backhand.Protocols.Padp.Internal;
using Backhand.Protocols.Slp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Protocols.Padp
{
    public class PadpConnection : IDlpTransport
    {
        public bool UseLongForm { get; set; } = false;

        private readonly SlpInterface _slpInterface;
        private readonly ArrayPool<byte> _arrayPool;

        private readonly byte _localSocketId;
        private readonly byte _remoteSocketId;
        private byte _transactionId;

        private ILogger _logger;

        private const byte PadpSlpPacketType = 0x02;
        private const int PadpMtu = sizeof(byte) * 1024;
        private const int PadpDefaultHeaderSize = sizeof(byte) * 4;
        private const int PadpLongHeaderSize = sizeof(byte) * 6;
        private static readonly TimeSpan AckTimeout = TimeSpan.FromSeconds(2);

        internal enum PadpFragmentType : byte
        {
            Data = 0x01,
            Ack = 0x02,
            Tickle = 0x04,
        }

        [Flags]
        internal enum PadpFragmentFlags : byte
        {
            None = 0b00000000,
            First = 0b10000000,
            Last = 0b01000000,
            MemoryError = 0b00100000,
            IsLongForm = 0b00010000,
        }

        public PadpConnection(SlpInterface slpInterface, byte localSocketId, byte remoteSocketId, byte initialTransactionId = 0xFF, ArrayPool<byte>? arrayPool = null, ILogger? logger = null)
        {
            _slpInterface = slpInterface;
            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
            _localSocketId = localSocketId;
            _remoteSocketId = remoteSocketId;
            _transactionId = initialTransactionId;
            _logger = logger ?? NullLogger.Instance;
        }

        public async Task ExecuteTransactionAsync(ReadOnlySequence<byte> sendPayload, Action<ReadOnlySequence<byte>> handleReceivePayloadFunc, CancellationToken cancellationToken = default)
        {
            byte transactionId = GetNewTransactionId();
            Task receivePayloadTask = ReceivePayloadAsync(transactionId, handleReceivePayloadFunc, cancellationToken);
            await SendPayloadAsync(transactionId, sendPayload, cancellationToken).ConfigureAwait(false);
            await receivePayloadTask.ConfigureAwait(false);
        }

        private byte GetNewTransactionId()
        {
            if (++_transactionId is 0xFF or 0x00)
            {
                _transactionId = 0x01;
            }
            return _transactionId;
        }

        public async Task SendPayloadAsync(byte transactionId, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            _logger.SendingPayload(transactionId, buffer);

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

                Task ackWaitTask = WaitForAckAsync(transactionId, sizeOrOffset, cancellationToken);
                EnqueueFragment(transactionId, PadpFragmentType.Data, flags, sizeOrOffset, buffer.Slice(offset, txSize));
                await ackWaitTask.ConfigureAwait(false);
            }
        }

        public async Task ReceivePayloadAsync(byte transactionId, Action<ReadOnlySequence<byte>> callback, CancellationToken cancellationToken)
        {
            _logger.WaitingForPayload(transactionId);

            TaskCompletionSource receiveTcs = new();
            using SegmentBuffer<byte> payloadBuffer = new(_arrayPool);

            void OnSlpPacketReceived(object? sender, SlpTransmissionEventArgs e)
            {
                // Ignore any packets that aren't the correct PADP type or have incorrect sockets
                if (e.Packet.PacketType != PadpSlpPacketType || e.Packet.DestinationSocket != _localSocketId || e.Packet.SourceSocket != _remoteSocketId)
                {
                    return;
                }

                // Parse header
                (byte packetTransactionId, PadpFragmentType packetType, PadpFragmentFlags packetFlags, uint packetSizeOrOffset, ReadOnlySequence<byte> packetBody) =
                    ReadFragment(e.Packet);

                _logger.ReceivedFragment(packetTransactionId, packetType, packetFlags, packetSizeOrOffset, packetBody);

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
                        throw new PadpException(
                            "Received data fragment marked as first, but already had data in buffer");
                    }
                }
                else
                {
                    // Our buffer length should match the sizeOrOffset
                    if (payloadBuffer.Length != packetSizeOrOffset)
                    {
                        throw new PadpException("Received data fragment with unexpected sizeOrOffset");
                    }
                }

                // Copy current fragment data into buffer
                Span<byte> payloadBufferSegment = payloadBuffer.Append(Convert.ToInt32(packetBody.Length));
                packetBody.CopyTo(payloadBufferSegment);

                // Send ACK for the received fragment
                EnqueueAck(transactionId, packetFlags, packetSizeOrOffset);

                // If not the last data fragment, continue listening.
                if (!packetFlags.HasFlag(PadpFragmentFlags.Last))
                {
                    return;
                }

                receiveTcs.TrySetResult();
            }

            _slpInterface.PacketReceived += OnSlpPacketReceived;
            try
            {
                await using (cancellationToken.Register(() => { receiveTcs.TrySetCanceled(); }))
                {
                    await receiveTcs.Task.ConfigureAwait(false);
                    ReadOnlySequence<byte> payload = payloadBuffer.AsReadOnlySequence();
                    _logger.ReceivedPayload(transactionId, payload);
                    callback(payload);
                }
            }
            finally
            {
                _slpInterface.PacketReceived -= OnSlpPacketReceived;
            }
        }

        private void EnqueueAck(byte transactionId, PadpFragmentFlags flags, uint sizeOrOffset)
        {
            EnqueueFragment(transactionId, PadpFragmentType.Ack, flags, sizeOrOffset, ReadOnlySequence<byte>.Empty);
        }

        private async Task WaitForAckAsync(byte transactionId, uint sizeOrOffset, CancellationToken cancellationToken)
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

                    // Parse header
                    (byte packetTransactionId, PadpFragmentType packetType, PadpFragmentFlags packetFlags, uint packetSizeOrOffset, ReadOnlySequence<byte> packetBody) =
                        ReadFragment(e.Packet);

                    _logger.ReceivedFragment(packetTransactionId, packetType, packetFlags, packetSizeOrOffset, packetBody);

                    if (e.Packet.TransactionId != transactionId || packetType != PadpFragmentType.Ack || sizeOrOffset != packetSizeOrOffset)
                    {
                        // Maybe just log?
                        // throw new PadpException("Unexpected packet");
                        return;
                    }

                    ackTcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    ackTcs.TrySetException(ex);
                }
            }

            _slpInterface.PacketReceived += OnSlpPacketReceived;

            using CancellationTokenSource timeoutCts = new(AckTimeout);
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            try
            {
                await using (linkedCts.Token.Register(() => ackTcs.TrySetCanceled()))
                {
                    await ackTcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                _slpInterface.PacketReceived -= OnSlpPacketReceived;
            }
        }

        private void EnqueueFragment(byte transactionId, PadpFragmentType type, PadpFragmentFlags flags, uint sizeOrOffset, ReadOnlySequence<byte> data)
        {
            _logger.SendingFragment(transactionId, type, flags, sizeOrOffset, data);

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
                bufferWriter.Write(data);

                // Build SLP packet
                SlpPacket slpPacket = new()
                {
                    DestinationSocket = _remoteSocketId,
                    SourceSocket = _localSocketId,
                    PacketType = PadpSlpPacketType,
                    TransactionId = transactionId,
                    Data = new ReadOnlySequence<byte>(buffer).Slice(0, packetSize)
                };

                // Enqueue packet
                _slpInterface.EnqueuePacket(slpPacket);
            }
            finally
            {
                // Return rented buffer
                _arrayPool.Return(buffer);
            }
        }

        private static (byte transactionId, PadpFragmentType type, PadpFragmentFlags flags, uint sizeOrOffset, ReadOnlySequence<byte> body) ReadFragment(SlpPacket slpPacket)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(slpPacket.Data);

            if (bufferReader.Remaining < 2)
            {
                throw new PadpException("Not enough data to read fragment header");
            }

            PadpFragmentType type = (PadpFragmentType)bufferReader.Read();
            PadpFragmentFlags flags = (PadpFragmentFlags)bufferReader.Read();

            uint sizeOrOffset;
            if (flags.HasFlag(PadpFragmentFlags.IsLongForm))
            {
                if (bufferReader.Remaining < sizeof(uint))
                {
                    throw new PadpException("Not enough data to read fragment header");
                }

                sizeOrOffset = bufferReader.ReadUInt32BigEndian();
            }
            else
            {
                if (bufferReader.Remaining < sizeof(ushort))
                {
                    throw new PadpException("Not enough data to read fragment header");
                }

                sizeOrOffset = bufferReader.ReadUInt16BigEndian();
            }

            ReadOnlySequence<byte> body = bufferReader.UnreadSequence;
            
            return (slpPacket.TransactionId, type, flags, sizeOrOffset, body);
        }
    }
}
