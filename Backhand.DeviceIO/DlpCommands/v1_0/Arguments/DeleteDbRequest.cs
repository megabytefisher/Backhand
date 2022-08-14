using Backhand.DeviceIO.Dlp;
using System;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class DeleteDbRequest : DlpArgument
    {
        public byte CardId { get; init; }
        public string Name { get; init; } = string.Empty;

        public override int GetSerializedLength() =>
            sizeof(byte) +                          // CardId
            sizeof(byte) +                          // (Padding)
            GetNullTerminatedStringLength(Name);    // Name

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            buffer[offset] = CardId;
            offset += sizeof(byte);

            buffer[offset] = 0; // Padding
            offset += sizeof(byte);

            offset += WriteNullTerminatedString(buffer.Slice(offset), Name);

            return offset;
        }
    }
}
