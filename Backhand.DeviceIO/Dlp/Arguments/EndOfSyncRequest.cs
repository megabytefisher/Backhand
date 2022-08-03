using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp.Arguments
{
    public class EndOfSyncRequest : DlpArgument
    {
        public enum EndOfSyncStatus : ushort
        {
            Okay                = 0x00,
            OutOfMemoryError    = 0x01,
            UseCancelledError   = 0x02,
            UnknownError        = 0x03,
        }

        public EndOfSyncStatus Status { get; set; }

        public override int GetSerializedLength()
        {
            return 2;
        }

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(0, 2), (ushort)Status);
            offset += 2;

            return offset;
        }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
