using Backhand.DeviceIO.Utility;
using Backhand.Utility.Buffers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Backhand.DeviceIO.Slp
{
    public abstract class SlpDevice : IDisposable
    {
        protected class SlpSendJob : IDisposable
        {
            public int Length { get; private init; }
            public byte[] Buffer { get; private init; }
            
            public SlpSendJob(int length)
            {
                Length = length;
                Buffer = ArrayPool<byte>.Shared.Rent(length);
            }

            public void Dispose()
            {
                ArrayPool<byte>.Shared.Return(Buffer);
            }
        }

        public event EventHandler<SlpPacketTransmittedArgs>? ReceivedPacket;
        public event EventHandler<SlpPacketTransmittedArgs>? SendingPacket;

        protected BufferBlock<SlpSendJob> _sendQueue;

        protected ILogger _logger;
        protected bool _logDebugEnabled;
        protected bool _logTraceEnabled;

        // Constants
        protected const byte HeaderMagic1 = 0xBE;
        protected const byte HeaderMagic2 = 0xEF;
        protected const byte HeaderMagic3 = 0xED;
        protected const int PacketHeaderSize = 10;
        protected const int PacketFooterSize = 2;
        protected const int MinPacketSize = PacketHeaderSize + PacketFooterSize;

        public SlpDevice(ILogger? logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _sendQueue = new BufferBlock<SlpSendJob>();

            _logDebugEnabled = _logger.IsEnabled(LogLevel.Debug);
            _logTraceEnabled = _logger.IsEnabled(LogLevel.Trace);
        }

        public virtual void Dispose()
        {
            if (_sendQueue.TryReceiveAll(out IList<SlpSendJob>? sendJobs) && sendJobs != null)
            {
                foreach (SlpSendJob sendJob in sendJobs)
                {
                    sendJob.Dispose();
                }
            }
        }

        public abstract Task RunIOAsync(CancellationToken cancellationToken = default);

        public void SendPacket(SlpPacket packet)
        {
            if (_logDebugEnabled)
            {
                _logger.LogDebug($"Enqueueing packet; Dst: {packet.DestinationSocket}, Src: {packet.SourceSocket}, Type: {packet.PacketType}, TxId: {packet.TransactionId}, Body: [{HexSerialization.GetHexString(packet.Data)}]");
            }

            SendingPacket?.Invoke(this, new SlpPacketTransmittedArgs(packet));

            // Get buffer to hold the serialized packet
            int packetLength = MinPacketSize + Convert.ToInt32(packet.Data.Length);
            SlpSendJob sendJob = new SlpSendJob(packetLength);

            // Write packet to buffer
            WritePacket(packet, sendJob.Buffer);

            if (!_sendQueue.Post(sendJob))
            {
                throw new SlpException("Failed to enqueue send packet");
            }
        }

        protected SequencePosition ReadPackets(ReadOnlySequence<byte> buffer, ref bool firstPacket)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);

            // If first packet, we need to scan until we find a valid header.
            if (firstPacket)
            {
                bool packetFound = false;

                while (bufferReader.Remaining >= PacketHeaderSize)
                {
                    // Skip to first byte of header magic
                    if (!bufferReader.TryAdvanceTo(HeaderMagic1, false))
                    {
                        // Didn't find header, but we checked the whole sequence.
                        return buffer.End;
                    }

                    if (bufferReader.Remaining < PacketHeaderSize)
                    {
                        // Not enough data to confirm - try again next time at this position.
                        return bufferReader.Position;
                    }

                    // If remaining header doesn't match, continue the search.
                    if (bufferReader.Peek(1) != HeaderMagic2 ||
                        bufferReader.Peek(2) != HeaderMagic3)
                    {
                        bufferReader.Advance(1);
                        continue;
                    }

                    // Calculate header checksum..
                    byte calculatedChecksum = 0;
                    for (int i = 0; i < PacketHeaderSize - 1; i++)
                    {
                        calculatedChecksum += bufferReader.Read();
                    }

                    // Does it match?
                    byte packetChecksum = bufferReader.Read();
                    if (calculatedChecksum == packetChecksum)
                    {
                        // Rewind to beginning of packet..
                        firstPacket = false;
                        packetFound = true;
                        bufferReader.Rewind(PacketHeaderSize);
                        break;
                    }
                    else
                    {
                        // Rewind to just after first byte of header
                        bufferReader.Rewind(PacketHeaderSize - 1);
                        continue;
                    }
                }

                if (!packetFound)
                {
                    // Need more data
                    return buffer.Start;
                }
            }

            while (bufferReader.Remaining >= MinPacketSize)
            {
                SequencePosition packetStart = bufferReader.Position;

                // Read packet header
                byte magic1 = bufferReader.Read();
                byte magic2 = bufferReader.Read();
                byte magic3 = bufferReader.Read();
                byte destinationSocket = bufferReader.Read();
                byte sourceSocket = bufferReader.Read();
                byte packetType = bufferReader.Read();
                ushort dataSize = bufferReader.ReadUInt16BigEndian();
                byte transactionId = bufferReader.Read();
                byte headerChecksum = bufferReader.Read();

                if (magic1 != HeaderMagic1 || magic2 != HeaderMagic2 || magic3 != HeaderMagic3)
                {
                    throw new SlpException("Received unexpected packet magic");
                }

                byte computedHeaderChecksum = ComputeHeaderChecksum(destinationSocket, sourceSocket, packetType, dataSize, transactionId);
                if (computedHeaderChecksum != headerChecksum)
                {
                    throw new SlpException("Received invalid packet header checksum");
                }

                if (bufferReader.Remaining < dataSize + PacketFooterSize)
                {
                    // Not enough data - try parsing this packet again next time
                    return packetStart;
                }

                // Slice out packet body
                ReadOnlySequence<byte> packetBody = buffer.Slice(bufferReader.Position, dataSize);
                bufferReader.Advance(dataSize);

                // Read packet footer (the CRC16 checksum)
                ushort packetCrc16 = bufferReader.ReadUInt16BigEndian();

                // Calculate our own CRC16 hash over header + body
                ReadOnlySequence<byte> packetSequence = buffer.Slice(packetStart, PacketHeaderSize + dataSize);
                ushort calculatedCrc16 = Crc16.ComputeChecksum(packetSequence);

                if (packetCrc16 != calculatedCrc16)
                {
                    throw new SlpException("Received packet with invalid footer checksum");
                }

                SlpPacket packet = new SlpPacket
                {
                    DestinationSocket = destinationSocket,
                    SourceSocket = sourceSocket,
                    PacketType = packetType,
                    TransactionId = transactionId,
                    Data = packetBody
                };

                if (_logDebugEnabled)
                {
                    _logger.LogDebug($"Received packet; Dst: {packet.DestinationSocket}, Src: {packet.SourceSocket}, Type: {packet.PacketType}, TxId: {packet.TransactionId}, Body: [{HexSerialization.GetHexString(packet.Data)}]");
                }

                ReceivedPacket?.Invoke(this, new SlpPacketTransmittedArgs(packet));
            }

            return bufferReader.Position;
        }

        protected SequencePosition ReadPackets(ReadOnlySequence<byte> buffer)
        {
            bool firstPacket = false;
            return ReadPackets(buffer, ref firstPacket);
        }

        protected void WritePacket(SlpPacket packet, Span<byte> buffer)
        {
            // Write header
            buffer[0] = HeaderMagic1;
            buffer[1] = HeaderMagic2;
            buffer[2] = HeaderMagic3;
            buffer[3] = packet.DestinationSocket;
            buffer[4] = packet.SourceSocket;
            buffer[5] = packet.PacketType;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(6, 2), (ushort)packet.Data.Length);
            buffer[8] = packet.TransactionId;
            buffer[9] = ComputeHeaderChecksum(packet.DestinationSocket, packet.SourceSocket, packet.PacketType, (ushort)packet.Data.Length, packet.TransactionId);

            // Write body
            packet.Data.CopyTo(buffer.Slice(10, (int)packet.Data.Length));

            // Calculate crc hash over data we've written so far
            ushort crc16 = Crc16.ComputeChecksum(buffer.Slice(0, PacketHeaderSize + (int)packet.Data.Length));
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(10 + (int)packet.Data.Length, 2), crc16);
        }

        protected byte ComputeHeaderChecksum(byte destinationSocket, byte sourceSocket, byte packetType, ushort dataSize, byte transactionId)
        {
            byte dataSize0 = (byte)dataSize;
            byte dataSize1 = (byte)(dataSize >> 8);
            return (byte)(HeaderMagic1 + HeaderMagic2 + HeaderMagic3 + destinationSocket + sourceSocket + packetType + dataSize0 + dataSize1 + transactionId);
        }
    }
}
