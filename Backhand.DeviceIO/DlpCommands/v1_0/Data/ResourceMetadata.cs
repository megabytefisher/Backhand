using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Data
{
    public class ResourceMetadata : DlpArgument
    {
        public string Type { get; set; } = "";
        public ushort ResourceId { get; set; }
        public ushort Index { get; set; }
        public ushort Size { get; set; }

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
            Type = ReadFixedLengthString(ref bufferReader, 4);
            ResourceId = bufferReader.ReadUInt16BigEndian();
            Index = bufferReader.ReadUInt16BigEndian();
            Size = bufferReader.ReadUInt16BigEndian();
        }
    }
}
