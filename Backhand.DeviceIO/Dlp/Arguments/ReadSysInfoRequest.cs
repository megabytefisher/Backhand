using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp.Arguments
{
    public class ReadSysInfoRequest : DlpArgument
    {
        public ushort HostDlpVersionMajor { get; set; }
        public ushort HostDlpVersionMinor { get; set; }

        public override int GetSerializedLength()
        {
            return 4;
        }

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), HostDlpVersionMajor);
            offset += 2;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), HostDlpVersionMinor);
            offset += 2;

            return offset;
        }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
