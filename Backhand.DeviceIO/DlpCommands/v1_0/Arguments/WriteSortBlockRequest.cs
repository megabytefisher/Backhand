using System;
using System.Buffers.Binary;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backhand.DeviceIO.Dlp;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class WriteSortBlockRequest : DlpArgument
    {
        public byte DbHandle { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public override int GetSerializedLength()
        {
            return
                sizeof(byte) +      // DbHandle
                sizeof(byte) +      // (Padding)
                sizeof(ushort) +    // (Data Length)
                Data.Length;        // Data
        }

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            buffer[offset] = DbHandle;
            offset += 1;

            buffer[offset] = 0; // Padding
            offset += 1;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), Convert.ToUInt16(Data.Length));
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
