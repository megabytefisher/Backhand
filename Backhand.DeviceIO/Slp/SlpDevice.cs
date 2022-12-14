using Backhand.DeviceIO.Utility;
using Backhand.Utility.Buffers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Backhand.DeviceIO.Slp
{
    public sealed class SlpDevice : IDisposable
    {
        private class SlpSendJob : IDisposable
        {
            public int Length { get; }
            public byte[] Buffer { get; }
            
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

        private readonly IDuplexPipe _basePipe;
        private readonly BufferBlock<SlpSendJob> _sendQueue;

        private readonly ILogger _logger;
        private readonly bool _logDebugEnabled;
        private readonly bool _logTraceEnabled;
        private long _logSkipReadBytes;

        // Constants
        private const byte HeaderMagic1 = 0xBE;
        private const byte HeaderMagic2 = 0xEF;
        private const byte HeaderMagic3 = 0xED;
        private const int PacketHeaderSize = sizeof(byte) * 10;
        private const int PacketFooterSize = sizeof(byte) * 2;
        private const int MinPacketSize = PacketHeaderSize + PacketFooterSize;

        public SlpDevice(IDuplexPipe basePipe, ILogger? logger = null)
        {
            _basePipe = basePipe;
            _sendQueue = new BufferBlock<SlpSendJob>();

            _logger = logger ?? NullLogger.Instance;
            _logDebugEnabled = _logger.IsEnabled(LogLevel.Debug);
            _logTraceEnabled = _logger.IsEnabled(LogLevel.Trace);
        }

        public void Dispose()
        {
            if (_sendQueue.TryReceiveAll(out IList<SlpSendJob>? sendJobs))
            {
                foreach (SlpSendJob sendJob in sendJobs)
                {
                    sendJob.Dispose();
                }
            }
            _sendQueue.Complete();
        }

        public async Task RunIoAsync(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource abortCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, abortCts.Token);

            Task readerTask = RunReaderAsync(linkedCts.Token);
            Task writerTask = RunWriterAsync(linkedCts.Token);

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

        public void SendPacket(SlpPacket packet)
        {
            if (_logDebugEnabled)
            {
                _logger.LogDebug("Enqueueing packet; Dst: {destinationSocket}, Src: {sourceSocket}, Type: {packetType}, TxId: {transactionId}, Body: [{body}]",
                    packet.DestinationSocket,
                    packet.SourceSocket,
                    packet.PacketType,
                    packet.TransactionId,
                    HexSerialization.GetHexString(packet.Data));
            }

            SendingPacket?.Invoke(this, new SlpPacketTransmittedArgs(packet));

            // Get buffer to hold the serialized packet
            int packetLength = MinPacketSize + Convert.ToInt32(packet.Data.Length);
            SlpSendJob sendJob = new(packetLength);

            // Write packet to buffer
            WritePacket(packet, sendJob.Buffer);

            if (!_sendQueue.Post(sendJob))
            {
                throw new SlpException("Failed to enqueue send packet");
            }
        }

        private async Task RunReaderAsync(CancellationToken cancellationToken)
        {
            bool firstPacket = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult readResult = await _basePipe.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = readResult.Buffer;

                if (_logTraceEnabled)
                {
                    _logger.LogTrace("Received bytes: [{bytes}]",
                        HexSerialization.GetHexString(buffer.Slice(_logSkipReadBytes)));
                }

                SequencePosition processedPosition = ReadPackets(buffer, ref firstPacket);

                _basePipe.Input.AdvanceTo(processedPosition, buffer.End);

                if (_logTraceEnabled)
                {
                    ReadOnlySequence<byte> processedSequence = buffer.Slice(0, processedPosition);
                    _logSkipReadBytes = buffer.Length - processedSequence.Length;
                }
                
                if (readResult.IsCompleted)
                    break;
            }
        }

        private async Task RunWriterAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                SlpSendJob sendJob = await _sendQueue.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                if (_logTraceEnabled)
                {
                    _logger.LogTrace("Sending bytes: [{bytes}]]",
                        HexSerialization.GetHexString(((Span<byte>)sendJob.Buffer).Slice(0, sendJob.Length)));
                }
                
                Memory<byte> sendBuffer = _basePipe.Output.GetMemory(sendJob.Length);
                ((Span<byte>)sendJob.Buffer).Slice(0, sendJob.Length).CopyTo(sendBuffer.Span);
                _basePipe.Output.Advance(sendJob.Length);

                FlushResult flushResult = await _basePipe.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
                if (flushResult.IsCompleted)
                    break;
            }
        }

        private SequencePosition ReadPackets(ReadOnlySequence<byte> buffer, ref bool firstPacket)
        {
            SequenceReader<byte> bufferReader = new(buffer);

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

                    // Rewind to just after first byte of header
                    bufferReader.Rewind(PacketHeaderSize - 1);
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
                    _logger.LogError("Received unexpected packet magic");
                    throw new SlpException("Received unexpected packet magic");
                }

                byte computedHeaderChecksum = ComputeHeaderChecksum(destinationSocket, sourceSocket, packetType, dataSize, transactionId);
                if (computedHeaderChecksum != headerChecksum)
                {
                    _logger.LogError("Received invalid packet header checksum");
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
                    _logger.LogError("Received packet with invalid footer checksum");
                    throw new SlpException("Received packet with invalid footer checksum");
                }

                SlpPacket packet = new()
                {
                    DestinationSocket = destinationSocket,
                    SourceSocket = sourceSocket,
                    PacketType = packetType,
                    TransactionId = transactionId,
                    Data = packetBody
                };

                if (_logDebugEnabled)
                {
                    _logger.LogDebug("Received packet; Dst: {destinationSocket}, Src: {sourceSocket}, Type: {packetType}, TxId: {transactionId}, Body: [{body}]",
                        packet.DestinationSocket,
                        packet.SourceSocket,
                        packet.PacketType,
                        packet.TransactionId,
                        HexSerialization.GetHexString(packet.Data));
                }

                ReceivedPacket?.Invoke(this, new SlpPacketTransmittedArgs(packet));
            }

            return bufferReader.Position;
        }

        private static void WritePacket(SlpPacket packet, Span<byte> buffer)
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

        private static byte ComputeHeaderChecksum(byte destinationSocket, byte sourceSocket, byte packetType, ushort dataSize, byte transactionId)
        {
            byte dataSize0 = (byte)dataSize;
            byte dataSize1 = (byte)(dataSize >> 8);
            return (byte)(HeaderMagic1 + HeaderMagic2 + HeaderMagic3 + destinationSocket + sourceSocket + packetType + dataSize0 + dataSize1 + transactionId);
        }
    }
}
