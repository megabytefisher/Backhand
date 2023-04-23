using Backhand.Common.BinarySerialization;

namespace Backhand.Pdb.FileSerialization
{
    [GenerateBinarySerialization]
    internal partial class PdbResourceMetadata : IBinarySerializable
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
