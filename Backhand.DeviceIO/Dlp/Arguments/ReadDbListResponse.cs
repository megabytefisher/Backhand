using Backhand.DeviceIO.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp.Arguments
{
    public class ReadDbListResponse : DlpArgument
    {
        public ushort LastIndex { get; set; }
        public byte Flags { get; set; }
        public DlpDatabaseMetadata[] Metadata { get; set; } = Array.Empty<DlpDatabaseMetadata>();

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
