using Backhand.Common.BinarySerialization;

namespace Backhand.Pdb.Memo
{
    public class MemoRecord : DatabaseRecord
    {
        public override byte[] Data
        {
            get => ContentString.Bytes;
            set => ContentString.Bytes = value;
        }

        private NullTerminatedBinaryString ContentString { get; } = new();

        public string Content
        {
            get => ContentString.Value;
            set => ContentString.Value = value;
        }
    }
}
