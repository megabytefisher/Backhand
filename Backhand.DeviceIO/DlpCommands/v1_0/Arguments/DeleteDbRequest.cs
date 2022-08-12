using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0.Data;
using System;
using System.Buffers.Binary;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class DeleteDbRequest : DlpArgument
    {
        public byte CardId { get; set; }
        public string Name { get; set; } = string.Empty;

        public override int GetSerializedLength()
        {
            return
                sizeof(byte) +
                sizeof(byte) +
                Name.Length + 1;
        }

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

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
