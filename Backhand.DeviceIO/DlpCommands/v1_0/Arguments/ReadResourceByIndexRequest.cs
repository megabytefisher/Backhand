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
    public class ReadResourceByIndexRequest : DlpArgument
    {
        public byte DbHandle { get; set; }
        public ushort ResourceIndex { get; set; }
        public ushort Offset { get; set; }
        public ushort MaxLength { get; set; }

        public override int GetSerializedLength()
        {
            return
                sizeof(byte) +      // DbHandle
                sizeof(byte) +      // (Padding)
                sizeof(ushort) +    // ResourceIndex
                sizeof(ushort) +    // Offset
                sizeof(ushort);     // MaxLength
        }

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

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
