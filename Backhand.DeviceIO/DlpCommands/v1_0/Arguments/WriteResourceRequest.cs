using Backhand.DeviceIO.Dlp;
using System;
using System.Buffers.Binary;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class WriteResourceRequest : DlpArgument
    {
        public byte DbHandle { get; init; }
        public string Type { get; init; } = "";
        public ushort ResourceId { get; init; }
        public ushort Size { get; init; }
        public byte[] Data { get; init; } = Array.Empty<byte>();

        public override int GetSerializedLength() =>
            sizeof(byte) +                          // DbHandle
            sizeof(byte) +                          // (Padding)
            (sizeof(byte) * 4) +                    // Type
            sizeof(ushort) +                        // ResourceId
            sizeof(ushort) +                        // Size
            (sizeof(byte) * Data.Length);           // Data

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            buffer[offset] = DbHandle;
            offset += sizeof(byte);

            buffer[offset] = 0; // Padding
            offset += sizeof(byte);

            WriteFixedLengthString(buffer.Slice(offset, sizeof(byte) * 4), Type);
            offset += sizeof(byte) * 4;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), ResourceId);
            offset += sizeof(ushort);

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), Size);
            offset += sizeof(ushort);

            Data.CopyTo(buffer.Slice(offset));
            offset += sizeof(byte) * Data.Length;

            return offset;
        }
    }
}
