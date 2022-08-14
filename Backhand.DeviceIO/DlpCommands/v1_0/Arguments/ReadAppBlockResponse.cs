using Backhand.DeviceIO.Dlp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadAppBlockResponse : DlpArgument
    {
        public ushort Size { get; private set; }
        public byte[] Data { get; private set; } = Array.Empty<byte>();

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        private void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            Size = bufferReader.ReadUInt16BigEndian();

            Data = new byte[bufferReader.Remaining];
            bufferReader.Sequence.Slice(bufferReader.Position).CopyTo(Data);
        }
    }
}
