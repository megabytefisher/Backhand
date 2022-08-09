using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Buffers.Binary;
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

        private static readonly byte[] NetSyncHandshakeWakeup = new byte[]
        {
            0x90, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00,
            0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] NetSyncHandshakeRequest1 = new byte[]
        {
            0x12, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00,
            0x24, 0xff, 0xff, 0xff, 0xff, 0x3c, 0x00, 0x3c, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0xc0, 0xa8, 0x01, 0x21, 0x04, 0x27, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] NetSyncHandshakeResponse1 = new byte[]
        {
            0x92, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00,
            0x24, 0xff, 0xff, 0xff, 0xff, 0x00, 0x3c, 0x00, 0x3c, 0x40, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0xc0, 0xa8, 0xa5, 0x1e, 0x04, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] NetSyncHandshakeRequest2 = new byte[]
        {
            0x13, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00,
            0x20, 0xff, 0xff, 0xff, 0xff, 0x00, 0x3c, 0x00, 0x3c, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] NetSyncHandshakeResponse2 = new byte[]
        {
            0x93, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        public NetSyncDevice()
        {

        }

        public abstract void SendPacket(NetSyncPacket packet);

        public async Task DoNetSyncHandshake()
        {
            // Wait for wakeup
            //await WatchPackets((p) =>
            //{
            //    return p.Data.Length == NetSyncHandshakeWakeup.Length && p.TransactionId == 0xff;
            //});

            // Watch for first response
            Task response1Task = WatchPackets((p) =>
            {
                return p.Data.Length == NetSyncHandshakeResponse1.Length;
            });

            // Send first response
            SendPacket(new NetSyncPacket(0x02, new ReadOnlySequence<byte>(NetSyncHandshakeRequest1)));

            // Wait for first response
            await response1Task;

            // Watch for second response
            Task response2Task = WatchPackets((p) =>
            {
                return p.Data.Length == NetSyncHandshakeResponse2.Length;
            });

            // Send second response
            SendPacket(new NetSyncPacket(0x03, new ReadOnlySequence<byte>(NetSyncHandshakeRequest2)));

            // Wait for second response
            await response2Task;
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
                bufferReader.Advance(payloadLength);

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
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), (uint)packet.Data.Length);
            offset += 4;
            packet.Data.CopyTo(buffer.Slice(offset));
        }

        private async Task WatchPackets(Func<NetSyncPacket, bool> handler)
        {
            TaskCompletionSource taskCompletionSource = new TaskCompletionSource();

            Action<object?, NetSyncPacketTransmittedEventArgs> watcher = (s, e) =>
            {
                if (handler(e.Packet))
                    taskCompletionSource.TrySetResult();
            };

            ReceivedPacket += watcher.Invoke;
            await taskCompletionSource.Task;
            ReceivedPacket -= watcher.Invoke;
        }
    }
}
