using Backhand.DeviceIO.Slp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backhand.DeviceIO.Padp
{
    public class PadpConnection
    {
        private enum PadpFragmentType : byte
        {
            Data        = 0x01,
            Ack         = 0x02,
            Tickle      = 0x04,
        }

        [Flags]
        private enum PadpFragmentFlags : byte
        {
            None        = 0b00000000,
            First       = 0b10000000,
            Last        = 0b01000000,
            MemoryError = 0b00100000,
            IsLongForm  = 0b00010000,
        }

        public bool UseLongForm { get; set; } = false;

        private readonly SlpDevice _device;

        private readonly byte _localSocketId;
        private readonly byte _remoteSocketId;
        private byte _transactionId;

        // A private array pool is used here because there's a chance that
        // ReceivePayloadAsync never completes and thus never returns its
        // buffers to the pool. At least here, our pool will be subject to
        // garbage collection and any missed rentals will be as well.
        private readonly ArrayPool<byte> _byteArrayPool;

        private readonly ILogger _logger;
        private readonly bool _logDebugEnabled;
        private readonly bool _logTraceEnabled;

        private const byte PadpSlpPacketType = 0x02;
        private const int PadpMtu = sizeof(byte) * 1024;
        private const int PadpHeaderSize = sizeof(byte) * 4;
        private static readonly TimeSpan AckTimeout = TimeSpan.FromSeconds(2);

        public PadpConnection(SlpDevice device, byte localSocketId, byte remoteSocketId, byte initialTransactionId = 0xff, ILogger? logger = null)
        {
            _device = device;
            _localSocketId = localSocketId;
            _remoteSocketId = remoteSocketId;
            _transactionId = initialTransactionId;

            _byteArrayPool = ArrayPool<byte>.Create();

            _logger = logger ?? NullLogger.Instance;
            _logDebugEnabled = _logger.IsEnabled(LogLevel.Debug);
            _logTraceEnabled = _logger.IsEnabled(LogLevel.Trace);
        }

        public async Task ExecuteTransactionAsync(PadpPayload sendPayload, Action<PadpPayload> handleReceivePayloadFunc, CancellationToken cancellationToken = default)
        {
            BumpTransactionId();
            Task receivePayloadTask = ReceivePayloadAsync(handleReceivePayloadFunc, cancellationToken);
            await SendPayloadAsync(sendPayload, cancellationToken).ConfigureAwait(false);
            await receivePayloadTask.ConfigureAwait(false);
        }

        public async Task SendPayloadAsync(PadpPayload payload, CancellationToken cancellationToken)
        {
            if (_logDebugEnabled)
            {
                _logger.LogDebug("Sending payload; TxId: {transactionId}, Body: [{body}]",
                    _transactionId,
                    HexSerialization.GetHexString(payload.Buffer));
            }

            // Send data in fragments
            for (int offset = 0; offset < payload.Buffer.Length; offset += PadpMtu)
            {
                uint txSize = (uint)Math.Min(payload.Buffer.Length - offset, PadpMtu);

                bool first = offset == 0;
                bool last = offset + txSize >= payload.Buffer.Length;
                PadpFragmentFlags flags =
                    (first ? PadpFragmentFlags.First : PadpFragmentFlags.None) |
                    (last ? PadpFragmentFlags.Last : PadpFragmentFlags.None);

                ushort sizeOrOffset = first ? (ushort)payload.Buffer.Length : (ushort)offset;

                Task ackWaitTask = WaitForAckAsync(sizeOrOffset, cancellationToken);
                SendFragment(PadpFragmentType.Data, flags, sizeOrOffset, payload.Buffer.Slice(offset, txSize));

                await ackWaitTask.ConfigureAwait(false);
            }
        }

        public async Task ReceivePayloadAsync(Action<PadpPayload> handlePayloadFunc, CancellationToken cancellationToken)
        {
            TaskCompletionSource receiveTcs = new();

            SegmentBuffer payloadBuffer = new(_byteArrayPool);
            
            void SlpPacketReceived(object? sender, SlpPacketTransmittedArgs e)
            {
                try
                {
                    // Ignore any packets that aren't the correct PADP type
                    // or have incorrect sockets
                    if (e.Packet.PacketType != PadpSlpPacketType ||
                        e.Packet.DestinationSocket != _localSocketId ||
                        e.Packet.SourceSocket != _remoteSocketId)
                    {
                        return;
                    }

                    // Parse header
                    SequencePosition fragmentBodyPosition = ReadFragmentHeader(e.Packet.Data,
                        out PadpFragmentType type, out PadpFragmentFlags flags, out uint sizeOrOffset);

                    // Slice out body.
                    ReadOnlySequence<byte> fragmentBody = e.Packet.Data.Slice(fragmentBodyPosition);

                    // We're expecting a packet that matches our transaction ID and is data type
                    if (e.Packet.TransactionId != _transactionId)
                    {
                        _logger.LogWarning(
                            "Received unexpected fragment; TxId: {transactionId}, Type: {type} Flags: [{flags}], SizeOrOffset: {sizeOrOffset}, Body: [{body}]",
                            e.Packet.TransactionId,
                            type,
                            flags,
                            sizeOrOffset,
                            HexSerialization.GetHexString(fragmentBody));
                        return;
                    }

                    // We may receive ACKs at this point- just ignore.
                    if (type != PadpFragmentType.Data)
                    {
                        return;
                    }

                    if (_logTraceEnabled)
                    {
                        _logger.LogTrace(
                            "Received data fragment; TxId: {transactionId}, Flags: [{flags}], SizeOrOffset: {sizeOrOffset}, Body: [{body}]",
                            e.Packet.TransactionId,
                            flags,
                            sizeOrOffset,
                            HexSerialization.GetHexString(fragmentBody));
                    }

                    if (flags.HasFlag(PadpFragmentFlags.First))
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
                        if (payloadBuffer.Length != sizeOrOffset)
                        {
                            throw new PadpException("Received data fragment with unexpected sizeOrOffset");
                        }
                    }

                    // Copy body into payload buffer
                    Span<byte> payloadBufferSegment = payloadBuffer.Append(Convert.ToInt32(fragmentBody.Length));
                    fragmentBody.CopyTo(payloadBufferSegment);

                    // Send ACK
                    SendAck(flags, (ushort)sizeOrOffset);

                    if (!flags.HasFlag(PadpFragmentFlags.Last))
                        return;

                    ReadOnlySequence<byte> payloadSequence = payloadBuffer.AsSequence();

                    if (_logDebugEnabled)
                    {
                        _logger.LogDebug("Received payload; TxId: {transactionId}, Body: [{body}]",
                            _transactionId,
                            HexSerialization.GetHexString(payloadSequence));
                    }

                    handlePayloadFunc(new PadpPayload(payloadSequence));
                    payloadBuffer.Dispose();
                    receiveTcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    receiveTcs.TrySetException(ex);
                }
            }

            _device.ReceivedPacket += SlpPacketReceived;
            await using (cancellationToken.Register(receiveTcs.TrySetCanceled))
            {
                try
                {
                    await receiveTcs.Task.ConfigureAwait(false);
                }
                finally
                {
                    _device.ReceivedPacket -= SlpPacketReceived;
                }
            }
        }

        private void BumpTransactionId()
        {
            _transactionId++;
            if (_transactionId is 0xff or 0x00)
            {
                _transactionId = 0x01;
            }
        }

        private void SendFragment(PadpFragmentType type, PadpFragmentFlags flags, uint sizeOrOffset, ReadOnlySequence<byte> clientData)
        {
            if (_logTraceEnabled)
            {
                _logger.LogTrace("Sending fragment; TxId: {transactionId}, Type: {type}, Flags: [{flags}], SizeOrOffset: {sizeOrOffset}, Body: [{body}]",
                    _transactionId,
                    type,
                    flags,
                    sizeOrOffset,
                    HexSerialization.GetHexString(clientData));
            }
            
            // Get buffer to hold packet..
            int packetSize = Convert.ToInt32(clientData.Length + (UseLongForm ? 6 : 4));
            byte[] padpBuffer = _byteArrayPool.Rent(packetSize);

            // Write header
            int offset = 0;
            
            padpBuffer[offset] = (byte)type;
            offset += sizeof(byte);
            
            padpBuffer[offset] = (byte)flags;
            offset += sizeof(byte);

            if (UseLongForm)
            {
                BinaryPrimitives.WriteUInt32BigEndian(((Span<byte>)padpBuffer).Slice(offset, sizeof(uint)), sizeOrOffset);
                offset += sizeof(uint);
            }
            else
            {
                BinaryPrimitives.WriteUInt16BigEndian(((Span<byte>)padpBuffer).Slice(offset, sizeof(ushort)), Convert.ToUInt16(sizeOrOffset));
                offset += sizeof(ushort);
            }

            // Write client data
            clientData.CopyTo(((Span<byte>)padpBuffer).Slice(offset));

            // Build into SLP packet
            SlpPacket slpPacket = new()
            {
                DestinationSocket = _remoteSocketId,
                SourceSocket = _localSocketId,
                PacketType = PadpSlpPacketType,
                TransactionId = _transactionId,
                Data = new ReadOnlySequence<byte>(padpBuffer).Slice(0, packetSize)
            };

            // Send SLP packet
            _device.SendPacket(slpPacket);

            // Return buffer to pool
            _byteArrayPool.Return(padpBuffer);
        }

        private void SendAck(PadpFragmentFlags flags, ushort sizeOrOffset)
        {
            SendFragment(PadpFragmentType.Ack, flags, sizeOrOffset, ReadOnlySequence<byte>.Empty);
        }

        private async Task WaitForAckAsync(ushort sizeOrOffset, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource ackTcs = new();

            void SlpPacketReceived(object? sender, SlpPacketTransmittedArgs e)
            {
                try
                {
                    // Ignore any packets that aren't the correct PADP type
                    // or have incorrect sockets
                    if (e.Packet.PacketType != PadpSlpPacketType ||
                        e.Packet.DestinationSocket != _localSocketId ||
                        e.Packet.SourceSocket != _remoteSocketId)
                    {
                        return;
                    }

                    // Parse header
                    SequencePosition fragmentBodyPosition = ReadFragmentHeader(e.Packet.Data,
                        out PadpFragmentType type, out PadpFragmentFlags flags, out uint packetSizeOrOffset);

                    // Slice out body. Just used for logging.
                    ReadOnlySequence<byte> fragmentBody = e.Packet.Data.Slice(fragmentBodyPosition);

                    if (e.Packet.TransactionId != _transactionId ||
                        type != PadpFragmentType.Ack ||
                        packetSizeOrOffset != sizeOrOffset)
                    {
                        _logger.LogWarning(
                            "Received unexpected fragment; TxId: {transactionId}, Type: {type}, Flags: [{flags}], SizeOrOffset: {sizeOrOffset}, Body: [{body}]",
                            e.Packet.TransactionId,
                            type,
                            flags,
                            sizeOrOffset,
                            HexSerialization.GetHexString(fragmentBody));
                        return;
                    }

                    if (_logTraceEnabled)
                    {
                        _logger.LogTrace(
                            "Received ack fragment; TxId: {transactionId}, Flags: [{flags}], SizeOrOffset: {sizeOrOffset}, Body: [{body}]",
                            e.Packet.TransactionId,
                            flags,
                            sizeOrOffset,
                            HexSerialization.GetHexString(fragmentBody));
                    }

                    ackTcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    ackTcs.TrySetException(ex);
                }
            }

            _device.ReceivedPacket += SlpPacketReceived;

            using CancellationTokenSource timeoutCts = new(AckTimeout);
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await using (linkedCts.Token.Register(() =>
                         {
                             ackTcs.TrySetCanceled();
                         }))
            {
                try
                {
                    await ackTcs.Task.ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    if (timeoutCts.IsCancellationRequested)
                    {
                        throw new PadpException("Didn't receive PADP ack in time");
                    }
                }
                finally
                {
                    _device.ReceivedPacket -= SlpPacketReceived;
                }
            }
        }

        private static SequencePosition ReadFragmentHeader(ReadOnlySequence<byte> fragment, out PadpFragmentType type, out PadpFragmentFlags flags, out uint sizeOrOffset)
        {
            SequenceReader<byte> fragmentReader = new(fragment);
            if (fragmentReader.Remaining < 2)
                throw new PadpException("PADP fragment too small; could not read type/flags");

            type = (PadpFragmentType)fragmentReader.Read();
            flags = (PadpFragmentFlags)fragmentReader.Read();

            sizeOrOffset = flags.HasFlag(PadpFragmentFlags.IsLongForm) ?
                fragmentReader.ReadUInt32BigEndian() :
                fragmentReader.ReadUInt16BigEndian();

            return fragmentReader.Position;
        }
    }
}
