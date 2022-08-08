using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb.Internal
{
    public class FileEntryMetadataListHeader
    {
        public uint NextListId { get; set; }
        public ushort Length { get; set; }

        public const uint SerializedLength =
            sizeof(uint) +  // NextListId
            sizeof(ushort); // Length

        public void Serialize(Span<byte> buffer)
        {
            int offset = 0;

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), NextListId);
            offset += 4;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), Length);
            offset += 2;
        }

        public SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        public void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            NextListId = bufferReader.ReadUInt32BigEndian();
            Length = bufferReader.ReadUInt16BigEndian();
        }
    }
}
