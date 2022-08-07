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
    public class FileRecordMetadata
    {
        public uint LocalChunkId { get; set; }
        public RecordAttributes Attributes { get; set; }
        public uint UniqueId { get; set; }

        public const int SerializedLength =
            sizeof(uint) +                  // LocalChunkId
            sizeof(RecordAttributes) +      // Attributes
            (sizeof(byte) * 3);             // UniqueId

        public void Serialize(Span<byte> buffer)
        {
            int offset = 0;

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), LocalChunkId);
            offset += 4;

            buffer[offset] = (byte)Attributes;
            offset += 1;

            buffer[offset + 0] = (byte)(UniqueId >> 16);
            buffer[offset + 1] = (byte)(UniqueId >> 8);
            buffer[offset + 2] = (byte)(UniqueId >> 0);
            offset += 3;
        }

        public SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        public void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            LocalChunkId = bufferReader.ReadUInt32BigEndian();
            Attributes = (RecordAttributes)bufferReader.Read();
            UniqueId = (uint)((bufferReader.Read() << 16) |
                              (bufferReader.Read() << 8) |
                               bufferReader.Read());
        }
    }
}
