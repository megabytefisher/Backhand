using System;
using Backhand.DeviceIO.Dlp;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class OpenDbRequest : DlpArgument
    {
        [Flags]
        public enum OpenDbMode : byte
        {
            Read            = 0b10000000,
            Write           = 0b01000000,
            Exclusive       = 0b00100000,
            Secret          = 0b00010000,
        }

        public byte CardId { get; init; }
        public OpenDbMode Mode { get; init; }
        public string Name { get; init; } = "";

        public override int GetSerializedLength() =>
            sizeof(byte) +                          // CardId
            sizeof(byte) +                          // Mode
            GetNullTerminatedStringLength(Name);    // Name

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            buffer[offset] = CardId;
            offset += sizeof(byte);
            
            buffer[offset] = (byte)Mode;
            offset += sizeof(byte);
            
            offset += WriteNullTerminatedString(buffer.Slice(offset), Name);

            return offset;
        }
    }
}
