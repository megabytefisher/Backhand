using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0.Data;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadRecordByIdResponse : DlpArgument
    {
        public RecordMetadata Metadata { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();

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
            Metadata = new RecordMetadata();
            Metadata.Deserialize(ref bufferReader);
            Data = new byte[bufferReader.Remaining];
            bufferReader.Sequence.Slice(bufferReader.Position).CopyTo(Data);
        }
    }
}
