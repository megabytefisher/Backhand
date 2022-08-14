using Backhand.DeviceIO.Dlp;
using System;
using System.Buffers.Binary;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadDbListRequest : DlpArgument
    {
        [Flags]
        public enum ReadDbListMode : byte
        {
            ListRam         = 0b10000000,
            ListRom         = 0b01000000,
            ListMultiple    = 0b00100000,
        }

        public ReadDbListMode Mode { get; init; }
        public byte CardId { get; init; }
        public ushort StartIndex { get; init; }

        public override int GetSerializedLength() =>
            sizeof(byte) +                          // Mode
            sizeof(byte) +                          // CardId
            sizeof(ushort);                         // StartIndex

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            buffer[offset] = (byte)Mode;
            offset += sizeof(byte);
            
            buffer[offset] = CardId;
            offset += sizeof(byte);
            
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), StartIndex);
            offset += sizeof(ushort);

            return offset;
        }
    }
}
