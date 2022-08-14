using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Backhand.Pdb.Internal
{
    internal class FileEntryMetadataListHeader
    {
        public uint NextListId { get; set; }
        public ushort Length { get; set; }

        public const uint SerializedLength =
            sizeof(uint) +                      // NextListId
            sizeof(ushort);                     // Length

        public void Serialize(Span<byte> buffer)
        {
            int offset = 0;

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, sizeof(uint)), NextListId);
            offset += sizeof(uint);

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), Length);
            offset += sizeof(ushort);
        }

        public SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        private void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            NextListId = bufferReader.ReadUInt32BigEndian();
            Length = bufferReader.ReadUInt16BigEndian();
        }
    }
}
