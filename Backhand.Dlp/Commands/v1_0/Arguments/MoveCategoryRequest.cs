using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class MoveCategoryRequest : IBinarySerializable
    {
        [BinarySerialize]
        public byte DbHandle { get; set; }

        [BinarySerialize]
        public byte FromCategoryId { get; set; }

        [BinarySerialize]
        public byte ToCategoryId { get; set; }

        [BinarySerialize]
        private byte Padding { get; set; }
    }
}
