using Backhand.Pdb.Utility;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Backhand.Pdb.Internal
{
    public  class FileResourceMetadata
    {
        public string Type { get; set; } = "";
        public ushort ResourceId { get; set; }
        public uint LocalChunkId { get; set; }

        public const int SerializedLength =
            (sizeof(byte) * 4) +            // Type
            sizeof(ushort) +                // ResourceId
            sizeof(uint);                   // LocalChunkId

        public void Serialize(Span<byte> buffer)
        {
            int offset = 0;

            BufferUtilities.WriteFixedLengthString(buffer.Slice(offset, sizeof(byte) * 4), Type);
            offset += sizeof(byte) * 4;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), ResourceId);
            offset += sizeof(ushort);

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, sizeof(uint)), LocalChunkId);
            offset += sizeof(uint);
        }

        public SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        public void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            Type = BufferUtilities.ReadFixedLengthString(ref bufferReader, sizeof(byte) * 4);
            ResourceId = bufferReader.ReadUInt16BigEndian();
            LocalChunkId = bufferReader.ReadUInt32BigEndian();
        }
    }
}
