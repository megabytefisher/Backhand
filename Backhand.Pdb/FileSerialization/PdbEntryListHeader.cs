using Backhand.Common.BinarySerialization;

namespace Backhand.Pdb.FileSerialization
{
    [BinarySerializable]
    public class PdbEntryListHeader
    {
        [BinarySerialize]
        public uint NextListId { get; set; }

        [BinarySerialize]
        public ushort Length { get; set; }
    }
}
