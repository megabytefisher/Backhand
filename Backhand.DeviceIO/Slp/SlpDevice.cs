using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Backhand.DeviceIO.Utility;

namespace Backhand.DeviceIO.Slp
{
    public class SlpDevice : IDisposable
    {
        private class SlpSendJob : IDisposable
        {
            public int Length { get; set; }
            public byte[] Buffer { get; set; }

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

        private SerialPort _serialPort;

        private BufferBlock<SlpSendJob> _sendQueue;
        private CancellationTokenSource _workerCts;
        private ManualResetEventSlim _workerExited;

        private const byte HeaderMagic1 = 0xBE;
        private const byte HeaderMagic2 = 0xEF;
        private const byte HeaderMagic3 = 0xED;

        private const int PacketHeaderSize = 10;
        private const int PacketFooterSize = 2;
        private const int MinPacketSize = PacketHeaderSize + PacketFooterSize;

        public SlpDevice(string serialPortName)
        {
            _sendQueue = new BufferBlock<SlpSendJob>();
            _workerCts = new CancellationTokenSource();
            _workerExited = new ManualResetEventSlim();

            _serialPort = new SerialPort(serialPortName);
            _serialPort.BaudRate = 9600;
            _serialPort.Open();
        }

        public void Dispose()
        {
            _workerCts.Cancel();
            _serialPort.Dispose(); // dispose serial port early; the read will hang otherwise
            _workerExited.Wait();

            _workerCts.Dispose();
            _workerExited.Dispose();
        }

        public void SendPacket(SlpPacket packet)
        {
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

        public async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _workerCts.Token))
            {
                Task readerTask = RunReaderAsync(_workerCts.Token);
                Task writerTask = RunWriterAsync(_workerCts.Token);

                try
                {
                    await Task.WhenAny(readerTask, writerTask).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("EX");
                }
            }

            _workerExited.Set();
        }

        private async Task RunReaderAsync(CancellationToken cancellationToken)
        {
            bool firstPacket = true;
            PipeReader serialPortReader = PipeReader.Create(_serialPort.BaseStream);

            while (!cancellationToken.IsCancellationRequested)
            {
                // Attempt reading from serial port
                ReadResult readResult = await serialPortReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                if (readResult.IsCanceled)
                    break;

                ReadOnlySequence<byte> buffer = readResult.Buffer;

                // Read SLP packets from the buffer
                SequencePosition processedPosition = ReadPackets(buffer, ref firstPacket);

                // Advance the pipe
                serialPortReader.AdvanceTo(processedPosition, buffer.End);
            }
        }

        private async Task RunWriterAsync(CancellationToken cancellationToken)
        {
            PipeWriter serialPortWriter = PipeWriter.Create(_serialPort.BaseStream);

            while (!cancellationToken.IsCancellationRequested)
            {
                SlpSendJob sendJob = await _sendQueue.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                Memory<byte> sendBuffer = serialPortWriter.GetMemory(sendJob.Length);
                sendJob.Buffer.CopyTo(sendBuffer);

                serialPortWriter.Advance(sendJob.Length);
                await serialPortWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Dispose sendJob to return allocated array to pool
                sendJob.Dispose();
            }
        }

        private SequencePosition ReadPackets(ReadOnlySequence<byte> buffer, ref bool firstPacket)
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

                ReceivedPacket?.Invoke(this, new SlpPacketTransmittedArgs(packet));
            }

            return bufferReader.Position;
        }

        private void WritePacket(SlpPacket packet, Span<byte> buffer)
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

        private byte ComputeHeaderChecksum(byte destinationSocket, byte sourceSocket, byte packetType, ushort dataSize, byte transactionId)
        {
            byte dataSize0 = (byte)dataSize;
            byte dataSize1 = (byte)(dataSize >> 8);
            return (byte)(HeaderMagic1 + HeaderMagic2 + HeaderMagic3 + destinationSocket + sourceSocket + packetType + dataSize0 + dataSize1 + transactionId);
        }
    }
}
