using System;
using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0.Data;
using System.Buffers.Binary;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class CreateDbRequest : DlpArgument
    {
        public string Creator { get; init; } = "";
        public string Type { get; init; } = "";
        public byte CardId { get; init; }
        public DlpDatabaseAttributes Attributes { get; init; }
        public ushort Version { get; init; }
        public string Name { get; init; } = "";

        public override int GetSerializedLength() =>
            (sizeof(byte) * 4) +                    // Creator
            (sizeof(byte) * 4) +                    // Type
            sizeof(byte) +                          // CardId
            sizeof(byte) +                          // (Padding)
            sizeof(ushort) +                        // Attributes
            sizeof(ushort) +                        // Version
            GetNullTerminatedStringLength(Name);    // Name

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            WriteFixedLengthString(buffer.Slice(offset, sizeof(byte) * 4), Creator);
            offset += sizeof(byte) * 4;

            WriteFixedLengthString(buffer.Slice(offset, sizeof(byte) * 4), Type);
            offset += sizeof(byte) * 4;

            buffer[offset] = CardId;
            offset += sizeof(byte);

            buffer[offset] = 0; // Padding
            offset += sizeof(byte);

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), (ushort)Attributes);
            offset += sizeof(ushort);

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), Version);
            offset += sizeof(ushort);

            offset += WriteNullTerminatedString(buffer.Slice(offset), Name);

            return offset;
        }
    }
}
