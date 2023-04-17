using Backhand.Common.BinarySerialization;

namespace Backhand.Pdb.FileSerialization
{
    [BinarySerializable]
    internal class PdbResourceMetadata
    {
        [BinarySerialize]
        private FixedSizeBinaryString TypeString { get; set; } = new(4);

        [BinarySerialize]
        public ushort ResourceId { get; set; }

        [BinarySerialize]
        public uint LocalChunkId { get; set; }

        public string Type
        {
            get => TypeString.ToString();
            set => TypeString.Value = value;
        }
    }
}
