using Backhand.DeviceIO.Dlp;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public byte CardId { get; set; }
        public OpenDbMode Mode { get; set; }
        public string Name { get; set; } = "";

        public override int GetSerializedLength()
        {
            return 2 + Name.Length + 1;
        }

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;
            buffer[offset++] = CardId;
            buffer[offset++] = (byte)Mode;
            offset += WriteNullTerminatedString(buffer.Slice(offset), Name);

            return offset;
        }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
