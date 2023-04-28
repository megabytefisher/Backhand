using Backhand.Common.BinarySerialization;
using Backhand.Common.Buffers;
using System.Buffers;

namespace Backhand.PalmDb.Databases.Memo
{
    public class MemoRecord : IBinarySerializable
    {
        private NullTerminatedBinaryString ContentString { get; } = new();

        public string Content
        {
            get => ContentString.Value;
            set => ContentString.Value = value;
        }

        public int GetSize()
        {
            return ContentString.GetSize();
        }

        public void Write(ref SpanWriter<byte> writer)
        {
            BinarySerializer<NullTerminatedBinaryString>.Serialize(ContentString, ref writer);
        }

        public void Read(ref SequenceReader<byte> reader)
        {
            BinarySerializer<NullTerminatedBinaryString>.Deserialize(ref reader, ContentString);
        }
    }
}
