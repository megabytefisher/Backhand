using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Pdb.FileSerialization
{
    [GenerateBinarySerialization]
    internal partial class PdbEntryListHeader : IBinarySerializable
    {
        [BinarySerialize]
        public uint NextListId { get; set; }

        [BinarySerialize]
        public ushort Length { get; set; }
    }
}
