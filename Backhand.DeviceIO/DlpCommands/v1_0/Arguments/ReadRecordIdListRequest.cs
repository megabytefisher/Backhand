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
    public class ReadRecordIdListRequest : DlpArgument
    {
        [Flags]
        public enum ReadRecordIdListFlags : byte
        {
            ShouldSort          = 0b10000000,
        }

        public byte DbHandle { get; set; }
        public ReadRecordIdListFlags Flags { get; set; }
        public ushort StartIndex { get; set; }
        public ushort MaxRecords { get; set; }

        public override int GetSerializedLength()
        {
            return 6;
        }

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;
            buffer[offset++] = DbHandle;
            buffer[offset++] = (byte)Flags;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), StartIndex);
            offset += 2;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), MaxRecords);
            offset += 2;

            return offset;
        }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
