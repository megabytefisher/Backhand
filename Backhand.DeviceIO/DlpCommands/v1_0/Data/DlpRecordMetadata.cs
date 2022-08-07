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
        [Flags]
        public enum RecordFlags : byte
        {
            Delete          = 0b10000000,
            Dirty           = 0b01000000,
            Busy            = 0b00100000,
            Secret          = 0b00010000,
        }

        public uint RecordId { get; set; }
        public ushort Index { get; set; }
        public ushort Length { get; set; }
        public RecordFlags Flags { get; set; }
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
            Flags = (RecordFlags)bufferReader.Read();
            Category = bufferReader.Read();
        }
    }
}
