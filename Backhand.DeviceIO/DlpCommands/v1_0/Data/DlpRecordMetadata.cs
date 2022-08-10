using Backhand.DeviceIO.Dlp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Data
{
    public class DlpRecordMetadata : DlpArgument
    {
        public uint RecordId { get; set; }
        public ushort Index { get; set; }
        public ushort Length { get; set; }
        public DlpRecordAttributes Attributes { get; set; }
        public byte Category { get; set; }

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
            RecordId = bufferReader.ReadUInt32BigEndian();
            Index = bufferReader.ReadUInt16BigEndian();
            Length = bufferReader.ReadUInt16BigEndian();
            Attributes = (DlpRecordAttributes)bufferReader.Read();
            Category = bufferReader.Read();
        }
    }
}
