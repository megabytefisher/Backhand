using Backhand.DeviceIO.Dlp;
using System;
using System.Buffers.Binary;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadSortBlockRequest : DlpArgument
    {
        public byte DbHandle { get; set; }
        public ushort Offset { get; set; }
        public ushort Length { get; set; }

        public override int GetSerializedLength()
        {
            return
                sizeof(byte) +      // DbHandle
                sizeof(byte) +      // (Padding)
                sizeof(ushort) +    // Offset
                sizeof(ushort);     // Length
        }

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            buffer[offset] = DbHandle;
            offset += 1;

            buffer[offset] = 0; // Padding
            offset += 1;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), Offset);
            offset += 2;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), Length);
            offset += 2;

            return offset;
        }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
