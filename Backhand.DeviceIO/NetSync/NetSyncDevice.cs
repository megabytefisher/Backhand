using Backhand.DeviceIO.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.NetSync
{
    public abstract class NetSyncDevice
    {
        public event EventHandler<NetSyncPacketTransmittedEventArgs>? ReceivedPacket;

        protected const int NetSyncHeaderLength = 6;

        public abstract void SendPacket(NetSyncPacket packet);

        public virtual Task DoHandshake()
        {
            return Task.CompletedTask;
        }

        protected SequencePosition ReadPackets(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);
            
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
                ReceivedPacket?.Invoke(this, new NetSyncPacketTransmittedEventArgs(new NetSyncPacket(transactionId, payloadBuffer)));
            }

            return bufferReader.Position;
        }

        protected uint GetPacketLength(NetSyncPacket packet)
        {
            return Convert.ToUInt32(NetSyncHeaderLength + packet.Data.Length);
        }

        protected void WritePacket(NetSyncPacket packet, Span<byte> buffer)
        {
            int offset = 0;
            buffer[offset++] = 0x01; // data type
            buffer[offset++] = packet.TransactionId;
            packet.Data.CopyTo(buffer.Slice(offset));
        }
    }
}
