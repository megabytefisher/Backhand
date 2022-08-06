using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadRecordIdListResponse : DlpArgument
    {
        public uint[] RecordIds { get; set; } = Array.Empty<uint>();

        public override int GetSerializedLength()
        {
            throw new NotImplementedException();
        }

        public override int Serialize(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        public void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            ushort recordCount = bufferReader.ReadUInt16BigEndian();
            RecordIds = new uint[recordCount];
            for (ushort i = 0; i < recordCount; i++)
            {
                RecordIds[i] = bufferReader.ReadUInt32BigEndian();
            }
        }
    }
}
