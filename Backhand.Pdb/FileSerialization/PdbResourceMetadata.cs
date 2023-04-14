using Backhand.Common.BinarySerialization;

namespace Backhand.Pdb.FileSerialization
{
    [BinarySerializable]
    internal class PdbResourceMetadata
    {
        [BinarySerialize(Length = 4)]
        public string Type { get; set; } = string.Empty;

        [BinarySerialize]
        public ushort ResourceId { get; set; }

        [BinarySerialize]
        public uint LocalChunkId { get; set; }
    }
}
