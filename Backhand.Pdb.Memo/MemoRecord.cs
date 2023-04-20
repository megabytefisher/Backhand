using Backhand.Common.BinarySerialization;
using Backhand.Common.Buffers;
using System.Buffers;

namespace Backhand.Pdb.Memo
{
    public class MemoRecord : DatabaseRecord
    {
        private NullTerminatedBinaryString ContentString { get; } = new();

        public string Content
        {
            get => ContentString.Value;
            set => ContentString.Value = value;
        }

        public override int GetDataSize()
        {
            return ContentString.GetSize();
        }

        public override void WriteData(ref SpanWriter<byte> writer)
        {
            BinarySerializer<NullTerminatedBinaryString>.Serialize(ContentString, ref writer);
        }

        public override void ReadData(ref SequenceReader<byte> reader)
        {
            BinarySerializer<NullTerminatedBinaryString>.Deserialize(ref reader, ContentString);
        }
    }
}
