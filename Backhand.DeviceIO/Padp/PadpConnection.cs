﻿using Backhand.DeviceIO.Slp;
using Backhand.DeviceIO.Utility;
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

        public event EventHandler<PadpDataReceivedEventArgs>? DataReceived;
        public bool UseLongForm { get; set; }

        private SlpDevice _device;

        private byte _localSocketId;
        private byte _remoteSocketId;
        private byte _transactionId;

        private SegmentBuffer? _readBuffer;

        private const byte PadpSlpPacketType = 0x02;
        private const int PadpMtu = 1024;
        private const int PadpHeaderSize = 4;

        public PadpConnection(SlpDevice device, byte localSocketId, byte remoteSocketId, byte initialTransactionId)
        {
            _device = device;
            _localSocketId = localSocketId;
            _remoteSocketId = remoteSocketId;
            _transactionId = initialTransactionId;

            _device.PacketReceived += SlpPacketReceived;
        }

        public void Dispose()
        {
            _device.PacketReceived -= SlpPacketReceived;
        }

        public void BumpTransactionId()
        {
            _transactionId++;
            if (_transactionId == 0xff || _transactionId == 0x00)
            {
                _transactionId = 0x01;
            }
        }

        public async Task SendData(byte[] data)
        {
            for (int offset = 0; offset < data.Length; offset += PadpMtu)
            {
                int txSize = Math.Min(data.Length - offset, PadpMtu);

                bool first = offset == 0;
                bool last = offset + txSize >= data.Length;
                PadpFragmentFlags flags =
                    (first ? PadpFragmentFlags.First : PadpFragmentFlags.None) |
                    (last ? PadpFragmentFlags.Last : PadpFragmentFlags.None);

                ushort sizeOrOffset = first ? (ushort)data.Length : (ushort)offset;

                Task ackWaitTask = WaitForAckAsync(sizeOrOffset);
                SendFragment(PadpFragmentType.Data, flags, sizeOrOffset, ((Span<byte>)data).Slice(offset, txSize));

                await ackWaitTask;
            }
        }

        private void SlpPacketReceived(object? sender, SlpPacketReceivedEventArgs e)
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
                DataReceived?.Invoke(this, new PadpDataReceivedEventArgs(_readBuffer.AsSequence()));
                _readBuffer.Dispose();
                _readBuffer = null;
            }
        }

        private SequencePosition ReadFragmentHeader(ReadOnlySequence<byte> fragment, out PadpFragmentType type, out PadpFragmentFlags flags, out uint sizeOrOffset)
        {
            SequenceReader<byte> fragmentReader = new SequenceReader<byte>();
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

        private void SendFragment(PadpFragmentType type, PadpFragmentFlags flags, uint sizeOrOffset, Span<byte> clientData)
        {
            // Get buffer to hold packet..
            byte[] padpPacket = new byte[clientData.Length + (UseLongForm ? 6 : 4)];

            // Write header
            int offset = 0;
            padpPacket[offset++] = (byte)type;
            padpPacket[offset++] = (byte)flags;

            if (UseLongForm)
            {
                BinaryPrimitives.WriteUInt32BigEndian(((Span<byte>)padpPacket).Slice(offset, 4), sizeOrOffset);
                offset += 4;
            }
            else
            {
                BinaryPrimitives.WriteUInt16BigEndian(((Span<byte>)padpPacket).Slice(offset, 2), Convert.ToUInt16(sizeOrOffset));
                offset += 2;
            }

            clientData.CopyTo(((Span<byte>)padpPacket).Slice(offset));

            SlpPacket slpPacket = new SlpPacket
            {
                DestinationSocket = _remoteSocketId,
                SourceSocket = _localSocketId,
                PacketType = PadpSlpPacketType,
                TransactionId = _transactionId,
                Data = new ReadOnlySequence<byte>(padpPacket)
            };

            _device.SendPacket(slpPacket);
        }

        private void SendAck(PadpFragmentFlags flags, ushort sizeOrOffset)
        {
            SendFragment(PadpFragmentType.Ack, flags, sizeOrOffset, Span<byte>.Empty);
        }

        private async Task WaitForAckAsync(ushort sizeOrOffset)
        {
            using AsyncFlag ackFlag = new AsyncFlag();

            Action<object?, SlpPacketReceivedEventArgs> ackReceiver = (sender, e) =>
            {
                // Does the packet have correct destination/source socket? Is it a PADP packet?
                if (e.Packet.PacketType != PadpSlpPacketType || e.Packet.DestinationSocket != _localSocketId || e.Packet.SourceSocket != _remoteSocketId)
                    return;

                ReadFragmentHeader(e.Packet.Data, out PadpFragmentType type, out PadpFragmentFlags flags, out uint sizeOrOffset);

                // Ignore anything other than ack fragments
                if (type != PadpFragmentType.Ack)
                    return;

                // If its not for our transaction ID, ignore
                if (e.Packet.TransactionId != _transactionId)
                    return;

                ackFlag.Set();
            };

            _device.PacketReceived += ackReceiver.Invoke;
            await ackFlag.WaitAsync();
            _device.PacketReceived -= ackReceiver.Invoke;
        }
    }
}
