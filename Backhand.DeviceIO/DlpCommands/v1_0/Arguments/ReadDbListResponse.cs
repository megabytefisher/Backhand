using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0.Data;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadDbListResponse : DlpArgument
    {
        public ushort LastIndex { get; private set; }
        public byte Flags { get; private set; }
        public DlpDatabaseMetadata[] Metadata { get; private set; } = Array.Empty<DlpDatabaseMetadata>();

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        private void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            LastIndex = bufferReader.ReadUInt16BigEndian();
            Flags = bufferReader.Read();

            byte metadataCount = bufferReader.Read();
            Metadata = new DlpDatabaseMetadata[metadataCount];
            for (int i = 0; i < metadataCount; i++)
            {
                Metadata[i] = new DlpDatabaseMetadata();
                Metadata[i].Deserialize(ref bufferReader);
            }
        }
    }
}
