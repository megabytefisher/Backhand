using Backhand.DeviceIO.Dlp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadRecordIdListResponse : DlpArgument
    {
        public uint[] RecordIds { get; private set; } = Array.Empty<uint>();

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        private void Deserialize(ref SequenceReader<byte> bufferReader)
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
