using Backhand.DeviceIO.Dlp;
using System;
using System.Buffers.Binary;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadResourceByIndexRequest : DlpArgument
    {
        public byte DbHandle { get; init; }
        public ushort ResourceIndex { get; init; }
        public ushort Offset { get; init; }
        public ushort MaxLength { get; init; }

        public override int GetSerializedLength() =>
            sizeof(byte) +                          // DbHandle
            sizeof(byte) +                          // (Padding)
            sizeof(ushort) +                        // ResourceIndex
            sizeof(ushort) +                        // Offset
            sizeof(ushort);                         // MaxLength

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            buffer[offset] = DbHandle;
            offset += sizeof(byte);

            buffer[offset] = 0; // Padding
            offset += sizeof(byte);

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), ResourceIndex);
            offset += sizeof(ushort);

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), Offset);
            offset += sizeof(ushort);

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), MaxLength);
            offset += sizeof(ushort);

            return offset;
        }
    }
}
