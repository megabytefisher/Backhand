﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Backhand.Common.Buffers;
using Backhand.Common.Checksums;

namespace Backhand.Protocols.Slp
{
    public class SlpConnection : IDisposable
    {
        public event EventHandler<SlpTransmissionEventArgs>? ReceivedPacket;

        private readonly IDuplexPipe _basePipe;
        private readonly ArrayPool<byte> _arrayPool;
        private readonly BufferBlock<SendJob> _sendQueue;

        private readonly ILogger _logger;

        // Constants
        private const byte
            HeaderMagic1 = 0xBE,
            HeaderMagic2 = 0xEF,
            HeaderMagic3 = 0xED;
        private const int PacketHeaderSize =
            sizeof(byte) +      // Magic 1
            sizeof(byte) +      // Magic 2
            sizeof(byte) +      // Magic 3
            sizeof(byte) +      // Destination Socket
            sizeof(byte) +      // Source Socket
            sizeof(byte) +      // Packet Type
            sizeof(ushort) +    // Data Length
            sizeof(byte) +      // Transaction ID
            sizeof(byte);       // Header checksum
        private const int PacketFooterSize =
            sizeof(ushort);     // Footer checksum
        private const int PacketMinimumSize = PacketHeaderSize + PacketFooterSize;

        public SlpConnection(IDuplexPipe basePipe, ArrayPool<byte>? arrayPool = null, ILogger? logger = null)
        {
            _basePipe = basePipe;
            _logger = logger ?? NullLogger.Instance;
            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
            _sendQueue = new BufferBlock<SendJob>();
        }

        public void Dispose()
        {
            _sendQueue.Complete();

            if (_sendQueue.TryReceiveAll(out IList<SendJob>? sendJobs))
            {
                foreach (SendJob sendJob in sendJobs)
                {
                    sendJob.Dispose();
                }
            }
        }

        public async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource innerCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCts.Token);

            Task readTask = RunReadLoopAsync(cancellationToken);
            Task writeTask = RunWriteLoopAsync(cancellationToken);

            await Task.WhenAny(readTask, writeTask).ConfigureAwait(false);

            // When either task completes (in any state), make sure to cancel the other.
            innerCts.Cancel();
            await Task.WhenAll(readTask, writeTask).ConfigureAwait(false);
        }

        public void EnqueuePacket(SlpPacket packet)
        {
            // Enqueue the job.
            _logger.EnqueueingPacket(packet);
            _sendQueue.Post(CreateSendJob(packet));
        }

        private SendJob CreateSendJob(SlpPacket packet)
        {
            int packetLength = Convert.ToInt32(PacketHeaderSize + packet.Data.Length + PacketFooterSize);

            // Create a SendJob
            SendJob sendJob = new SendJob(_arrayPool, packetLength);
            WritePacket(packet, sendJob.Buffer);

            return sendJob;
        }

        private async Task RunReadLoopAsync(CancellationToken cancellationToken)
        {
            bool firstPacket = true;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ReadResult readResult = await _basePipe.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> readBuffer = readResult.Buffer;

                SequencePosition processedPosition = ReadPackets(readBuffer, ref firstPacket);
                _basePipe.Input.AdvanceTo(processedPosition, readBuffer.End);

                if (readResult.IsCompleted)
                    break;
            }
        }

        private async Task RunWriteLoopAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using SendJob job = await _sendQueue.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                job.Buffer.CopyTo(_basePipe.Output.GetSpan(job.Buffer.Length));
                _basePipe.Output.Advance(job.Buffer.Length);
                FlushResult flushResult = await _basePipe.Output.FlushAsync(cancellationToken).ConfigureAwait(false);

                if (flushResult.IsCompleted)
                    break;
            }
        }

        private SequencePosition ReadPackets(ReadOnlySequence<byte> buffer, ref bool firstPacket)
        {
            SequenceReader<byte> bufferReader = new(buffer);

            // If we're looking for the first packet, scan until a valid header is found.
            if (firstPacket)
            {
                bool packetFound = false;

                while (bufferReader.Remaining >= PacketHeaderSize)
                {
                    // Skip to the first byte of header magic
                    if (!bufferReader.TryAdvanceTo(HeaderMagic1, false))
                    {
                        // Didn't find header, but we checked the whole sequence.
                        return buffer.End;
                    }

                    if (bufferReader.Remaining < PacketHeaderSize)
                    {
                        // Not enough data to confirm it's valid - try again next time starting from the same location
                        return bufferReader.Position;
                    }

                    // If remaining header magic bytes don't match, try again, skipping the first byte.
                    if (bufferReader.Peek(1) != HeaderMagic2 ||
                        bufferReader.Peek(2) != HeaderMagic3)
                    {
                        bufferReader.Advance(1);
                        continue;
                    }

                    // Calculate the expected header checksum
                    byte calculatedChecksum = 0;
                    for (int i = 0; i < PacketHeaderSize - 1; i++)
                    {
                        calculatedChecksum += bufferReader.Read();
                    }

                    // Does it match?
                    byte packetChecksum = bufferReader.Read();
                    if (calculatedChecksum == packetChecksum)
                    {
                        // Rewind to beginning of packet
                        bufferReader.Rewind(PacketHeaderSize);
                        firstPacket = false;
                        packetFound = true;
                        break;
                    }

                    // Try again, skipping the first byte.
                    bufferReader.Rewind(PacketHeaderSize - 1);
                }

                if (!packetFound)
                {
                    // Need more data.
                    return buffer.Start;
                }
            }

            // Parse out packets.
            while (bufferReader.Remaining >= PacketMinimumSize)
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

                // Check header magic
                if (magic1 != HeaderMagic1 || magic2 != HeaderMagic2 || magic3 != HeaderMagic3)
                {
                    throw new SlpException("Packet header magic mismatch");
                }

                // Check header checksum
                byte computedHeaderChecksum = ComputeHeaderChecksum(destinationSocket, sourceSocket, packetType, dataSize, transactionId);
                if (computedHeaderChecksum != headerChecksum)
                {
                    throw new SlpException("Packet header checksum mismatch");
                }

                if (bufferReader.Remaining < dataSize + PacketFooterSize)
                {
                    // Not enough data - try parsing this packet again when we have more data.
                    return packetStart;
                }

                // Slice out packet body
                ReadOnlySequence<byte> packetBody = buffer.Slice(bufferReader.Position, dataSize);
                bufferReader.Advance(dataSize);

                // Read packet footer (CRC16 checksum of header + data)
                ushort footerChecksum = bufferReader.ReadUInt16BigEndian();

                // Check footer checksum
                ushort computedFooterChecksum = Crc16.ComputeChecksum(buffer.Slice(packetStart, PacketHeaderSize + dataSize));
                if (computedFooterChecksum != footerChecksum)
                {
                    throw new SlpException("Packet footer checksum mismatch");
                }

                SlpPacket receivedPacket = new()
                {
                    DestinationSocket = destinationSocket,
                    SourceSocket = sourceSocket,
                    PacketType = packetType,
                    TransactionId = transactionId,
                    Data = packetBody
                };

                _logger.ReceivedPacket(receivedPacket);
                ReceivedPacket?.Invoke(this, new SlpTransmissionEventArgs(receivedPacket));
            }

            return bufferReader.Position;
        }

        private static void WritePacket(SlpPacket packet, Span<byte> buffer)
        {
            SpanWriter<byte> bufferWriter = new(buffer);

            // Write header
            bufferWriter.Write(HeaderMagic1);
            bufferWriter.Write(HeaderMagic2);
            bufferWriter.Write(HeaderMagic3);
            bufferWriter.Write(packet.DestinationSocket);
            bufferWriter.Write(packet.SourceSocket);
            bufferWriter.Write(packet.PacketType);
            bufferWriter.WriteUInt16BigEndian(Convert.ToUInt16(packet.Data.Length));
            bufferWriter.Write(packet.TransactionId);
            bufferWriter.Write(ComputeHeaderChecksum(packet.DestinationSocket, packet.SourceSocket, packet.PacketType, Convert.ToUInt16(packet.Data.Length), packet.TransactionId));

            // Write body
            bufferWriter.WriteRange(packet.Data);

            // Write footer (CRC16 of header + data)
            bufferWriter.WriteUInt16BigEndian(Crc16.ComputeChecksum(buffer.Slice(0, PacketHeaderSize + (int)packet.Data.Length)));
        }

        private static byte ComputeHeaderChecksum(byte destinationSocket, byte sourceSocket, byte packetType, ushort dataSize, byte transactionId)
        {
            byte dataSize0 = (byte)dataSize;
            byte dataSize1 = (byte)(dataSize >> 8);
            return (byte)(HeaderMagic1 + HeaderMagic2 + HeaderMagic3 + destinationSocket + sourceSocket + packetType + dataSize0 + dataSize1 + transactionId);
        }

        private class SendJob : IDisposable
        {
            public Span<byte> Buffer => new(_array, 0, _length);

            private readonly ArrayPool<byte> _arrayPool;
            private readonly int _length;
            private readonly byte[] _array;

            public SendJob(ArrayPool<byte> arrayPool, int length)
            {
                _arrayPool = arrayPool;
                _length = length;
                _array = arrayPool.Rent(length);
            }

            public void Dispose()
            {
                _arrayPool.Return(_array);
            }
        }
    }
}
