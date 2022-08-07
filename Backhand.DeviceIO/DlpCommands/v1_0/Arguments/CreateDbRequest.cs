using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0.Data;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class CreateDbRequest : DlpArgument
    {
        public string Creator { get; set; } = "";
        public string Type { get; set; } = "";
        public byte CardId { get; set; }
        public DlpDatabaseAttributes Attributes { get; set; }
        public ushort Version { get; set; }
        public string Name { get; set; } = "";

        public override int GetSerializedLength()
        {
            return
                4 +
                4 +
                sizeof(byte) +
                sizeof(byte) +
                sizeof(DlpDatabaseAttributes) +
                sizeof(ushort) +
                Name.Length + 1;
        }

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            WriteFixedLengthString(buffer.Slice(offset, 4), Creator);
            offset += 4;

            WriteFixedLengthString(buffer.Slice(offset, 4), Type);
            offset += 4;

            buffer[offset] = CardId;
            offset += 1;

            buffer[offset] = 0; // Padding
            offset += 1;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)Attributes);
            offset += 2;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), Version);
            offset += 2;

            offset += WriteNullTerminatedString(buffer.Slice(offset), Name);

            return offset;
        }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
