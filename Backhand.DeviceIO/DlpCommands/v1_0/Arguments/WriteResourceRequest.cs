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
    public class WriteResourceRequest : DlpArgument
    {
        public byte DbHandle { get; set; }
        public string Type { get; set; } = "";
        public ushort ResourceId { get; set; }
        public ushort Size { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public override int GetSerializedLength()
        {
            return
                sizeof(byte) +
                sizeof(byte) +
                4 +
                sizeof(ushort) +
                sizeof(ushort) +
                Data.Length;
        }

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            buffer[offset] = DbHandle;
            offset += 1;

            buffer[offset] = 0; // Padding
            offset += 1;

            WriteFixedLengthString(buffer.Slice(offset, 4), Type);
            offset += 4;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), ResourceId);
            offset += 2;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), Size);
            offset += 2;

            Data.CopyTo(buffer.Slice(offset));
            offset += Data.Length;

            return offset;
        }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
