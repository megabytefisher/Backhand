using Backhand.DeviceIO.Slp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Padp
{
    public class PadpConnection : IDisposable
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

        public event EventHandler<PadpDataReceivedEventArgs>? ReceivedData;
        public bool UseLongForm { get; set; } = false;

        private SlpDevice _device;

        private byte _localSocketId;
        private byte _remoteSocketId;
        private byte _transactionId;

        private SegmentBuffer? _readBuffer;

        private const byte PadpSlpPacketType = 0x02;
        private const int PadpMtu = 1024;
        private const int PadpHeaderSize = 4;
        private static readonly TimeSpan AckTimeout = TimeSpan.FromSeconds(1);

        public PadpConnection(SlpDevice device, byte localSocketId, byte remoteSocketId, byte initialTransactionId)
        {
            _device = device;
            _localSocketId = localSocketId;
            _remoteSocketId = remoteSocketId;
            _transactionId = initialTransactionId;

            _device.ReceivedPacket += SlpPacketReceived;
        }

        public void Dispose()
        {
            _device.ReceivedPacket -= SlpPacketReceived;
            _readBuffer?.Dispose();
        }

        public void BumpTransactionId()
        {
            _transactionId++;
            if (_transactionId == 0xff || _transactionId == 0x00)
            {
                _transactionId = 0x01;
            }
        }

        public async Task SendData(ReadOnlySequence<byte> data, CancellationToken cancellationToken = default)
        {
            BumpTransactionId();
            for (int offset = 0; offset < data.Length; offset += PadpMtu)
            {
                uint txSize = (uint)Math.Min(data.Length - offset, PadpMtu);

                bool first = offset == 0;
                bool last = offset + txSize >= data.Length;
                PadpFragmentFlags flags =
                    (first ? PadpFragmentFlags.First : PadpFragmentFlags.None) |
                    (last ? PadpFragmentFlags.Last : PadpFragmentFlags.None);

                ushort sizeOrOffset = first ? (ushort)data.Length : (ushort)offset;

                Task ackWaitTask = WaitForAckAsync(sizeOrOffset, cancellationToken);
                SendFragment(PadpFragmentType.Data, flags, sizeOrOffset, data.Slice(offset, txSize));

                await ackWaitTask;
            }
        }

        private void SlpPacketReceived(object? sender, SlpPacketTransmittedArgs e)
        {
            if (e.Packet.PacketType != PadpSlpPacketType || e.Packet.DestinationSocket != _localSocketId || e.Packet.SourceSocket != _remoteSocketId)
                return;

            SequencePosition fragmentBodyPosition = ReadFragmentHeader(e.Packet.Data, out PadpFragmentType type, out PadpFragmentFlags flags, out uint sizeOrOffset);

            // We only care about data fragments here
            if (type != PadpFragmentType.Data)
                return;

            if (flags.HasFlag(PadpFragmentFlags.First))
            {
                if (_readBuffer != null)
                    throw new PadpException("Received unexpected first PADP data fragment");

                _readBuffer = new SegmentBuffer();
            }

            if (_readBuffer == null)
                throw new PadpException("Got PADP data fragment without receiving first packet");

            ReadOnlySequence<byte> fragmentClientData = e.Packet.Data.Slice(fragmentBodyPosition);
            Span<byte> readBufferSegment = _readBuffer.Append((int)fragmentClientData.Length);
            fragmentClientData.CopyTo(readBufferSegment);

            // Send ACK
            SendAck(flags, (ushort)sizeOrOffset);

            // Last packet? Publish data.
            if (flags.HasFlag(PadpFragmentFlags.Last))
            {
                ReceivedData?.Invoke(this, new PadpDataReceivedEventArgs(_readBuffer.AsSequence()));
                _readBuffer.Dispose();
                _readBuffer = null;
            }
        }

        private SequencePosition ReadFragmentHeader(ReadOnlySequence<byte> fragment, out PadpFragmentType type, out PadpFragmentFlags flags, out uint sizeOrOffset)
        {
            SequenceReader<byte> fragmentReader = new SequenceReader<byte>(fragment);
            if (fragmentReader.Remaining < 2)
                throw new PadpException("PADP fragment too small; could not read type/flags");

            type = (PadpFragmentType)fragmentReader.Read();
            flags = (PadpFragmentFlags)fragmentReader.Read();

            if (flags.HasFlag(PadpFragmentFlags.IsLongForm))
            {
                sizeOrOffset = fragmentReader.ReadUInt32BigEndian();
            }
            else
            {
                sizeOrOffset = fragmentReader.ReadUInt16BigEndian();
            }

            return fragmentReader.Position;
        }

        private void SendFragment(PadpFragmentType type, PadpFragmentFlags flags, uint sizeOrOffset, ReadOnlySequence<byte> clientData)
        {
            // Get buffer to hold packet..
            int packetSize = Convert.ToInt32(clientData.Length + (UseLongForm ? 6 : 4));
            byte[] padpBuffer = ArrayPool<byte>.Shared.Rent(packetSize);

            // Write header
            int offset = 0;
            padpBuffer[offset++] = (byte)type;
            padpBuffer[offset++] = (byte)flags;

            if (UseLongForm)
            {
                BinaryPrimitives.WriteUInt32BigEndian(((Span<byte>)padpBuffer).Slice(offset, 4), sizeOrOffset);
                offset += 4;
            }
            else
            {
                BinaryPrimitives.WriteUInt16BigEndian(((Span<byte>)padpBuffer).Slice(offset, 2), Convert.ToUInt16(sizeOrOffset));
                offset += 2;
            }

            // Write client data
            clientData.CopyTo(((Span<byte>)padpBuffer).Slice(offset));

            // Build into SLP packet
            SlpPacket slpPacket = new SlpPacket
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
            ArrayPool<byte>.Shared.Return(padpBuffer);
        }

        private void SendAck(PadpFragmentFlags flags, ushort sizeOrOffset)
        {
            SendFragment(PadpFragmentType.Ack, flags, sizeOrOffset, ReadOnlySequence<byte>.Empty);
        }

        private async Task WaitForAckAsync(ushort sizeOrOffset, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource ackTcs = new TaskCompletionSource();

            Action<object?, SlpPacketTransmittedArgs> ackReceiver = (sender, e) =>
            {
                // Does the packet have correct destination/source socket? Is it a PADP packet?
                if (e.Packet.PacketType != PadpSlpPacketType || e.Packet.DestinationSocket != _localSocketId || e.Packet.SourceSocket != _remoteSocketId)
                    return;

                ReadFragmentHeader(e.Packet.Data, out PadpFragmentType type, out PadpFragmentFlags flags, out uint packetSizeOrOffset);

                if (type != PadpFragmentType.Ack ||
                    e.Packet.TransactionId != _transactionId ||
                    packetSizeOrOffset != sizeOrOffset)
                {
                    return;
                }

                ackTcs.TrySetResult();
            };

            _device.ReceivedPacket += ackReceiver.Invoke;

            using CancellationTokenSource timeoutCts = new CancellationTokenSource(AckTimeout);
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            using (linkedCts.Token.Register(() =>
            {
                ackTcs.TrySetCanceled();
            }))
            {
                try
                {
                    await ackTcs.Task;
                }
                catch (TaskCanceledException ex)
                {
                    if (timeoutCts.IsCancellationRequested)
                    {
                        throw new PadpException("Didn't receive PADP ack in time");
                    }
                }
                finally
                {
                    _device.ReceivedPacket -= ackReceiver.Invoke;
                }
            }
        }
    }
}
