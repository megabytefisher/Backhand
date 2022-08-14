using System.Buffers;

namespace Backhand.DeviceIO.Slp
{
    public class SlpPacket
    {
        public byte DestinationSocket { get; init; }
        public byte SourceSocket { get; init; }
        public byte PacketType { get; init; }
        public byte TransactionId { get; init; }
        public ReadOnlySequence<byte> Data { get; init; }
    }
}
