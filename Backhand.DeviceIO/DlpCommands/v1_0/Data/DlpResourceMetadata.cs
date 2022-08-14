using Backhand.DeviceIO.Dlp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Data
{
    public class DlpResourceMetadata : DlpArgument
    {
        public string Type { get; private set; } = "";
        public ushort ResourceId { get; private set; }
        public ushort Index { get; private set; }
        public ushort Size { get; private set; }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
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
