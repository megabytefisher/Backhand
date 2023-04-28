using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.PalmDb.FileIO.FileSerialization
{
    [GenerateBinarySerialization]
    public partial class PdbEntryListHeader : IBinarySerializable
    {
        [BinarySerialize]
        public uint NextListId { get; set; }

        [BinarySerialize]
        public ushort Length { get; set; }

        public PdbEntryListHeader()
        {
        }

        public PdbEntryListHeader(ushort length)
        {
            Length = length;
        }
    }
}
