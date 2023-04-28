using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.PalmDb.FileIO.FileSerialization
{
    [GenerateBinarySerialization]
    public partial class PdbResourceMetadata : IBinarySerializable
    {
        [BinarySerialize]
        private FixedSizeBinaryString TypeString { get; } = new(4);

        [BinarySerialize]
        public ushort Id { get; set; }

        [BinarySerialize]
        public uint LocalChunkId { get; set; }

        public string Type
        {
            get => TypeString;
            set => TypeString.Value = value;
        }

        public PdbResourceMetadata()
        {
        }

        public PdbResourceMetadata(PalmDbResourceHeader sourceHeader, uint localChunkId)
        {
            Type = sourceHeader.Type;
            Id = sourceHeader.Id;
            LocalChunkId = localChunkId;
        }
        
        public PalmDbResourceHeader ToPalmDbResourceHeader()
        {
            return new PalmDbResourceHeader
            {
                Type = Type,
                Id = Id
            };
        }
    }
}
