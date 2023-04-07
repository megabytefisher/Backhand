using System.Buffers;

namespace Backhand.DeviceIO.Slp
{
    public class SlpPacket
    {
        public byte DestinationSocket { get; set; }
        public byte SourceSocket { get; set; }
        public byte PacketType { get; set; }
        public byte TransactionId { get; set; }
        public ReadOnlySequence<byte> Data { get; set; }
    }
}
