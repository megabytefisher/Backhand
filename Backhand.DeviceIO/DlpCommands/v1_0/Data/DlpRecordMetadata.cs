using Backhand.DeviceIO.Dlp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Data
{
    public class DlpRecordMetadata : DlpArgument
    {
        public uint RecordId { get; private set; }
        public ushort Index { get; private set; }
        public ushort Length { get; private set; }
        public DlpRecordAttributes Attributes { get; private set; }
        public byte Category { get; private set; }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
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
