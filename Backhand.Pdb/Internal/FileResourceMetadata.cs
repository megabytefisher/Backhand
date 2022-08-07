using Backhand.Pdb.Utility;
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
    public  class FileResourceMetadata
    {
        public string Type { get; set; } = "";
        public ushort ResourceId { get; set; } = 0;
        public uint LocalChunkId { get; set; }

        public const int SerializedLength =
            4 +                 // Type
            sizeof(ushort) +    // ResourceId
            sizeof(uint);       // LocalChunkId

        public void Serialize(Span<byte> buffer)
        {
            int offset = 0;

            BufferUtilities.WriteFixedLengthString(buffer.Slice(offset, 4), Type);
            offset += 4;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), ResourceId);
            offset += 2;

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), LocalChunkId);
            offset += 4;
        }

        public SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        public void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            Type = BufferUtilities.ReadFixedLengthString(ref bufferReader, 4);
            ResourceId = bufferReader.ReadUInt16BigEndian();
            LocalChunkId = bufferReader.ReadUInt32BigEndian();
        }
    }
}
