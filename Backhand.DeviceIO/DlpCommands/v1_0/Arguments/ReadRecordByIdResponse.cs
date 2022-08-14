using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0.Data;
using System;
using System.Buffers;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadRecordByIdResponse : DlpArgument
    {
        public DlpRecordMetadata Metadata { get; private set; } = new();
        public byte[] Data { get; private set; } = Array.Empty<byte>();

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        private void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            Metadata = new DlpRecordMetadata();
            Metadata.Deserialize(ref bufferReader);
            Data = new byte[bufferReader.Remaining];
            bufferReader.Sequence.Slice(bufferReader.Position).CopyTo(Data);
        }
    }
}
