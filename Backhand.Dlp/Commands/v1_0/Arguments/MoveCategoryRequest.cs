using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class MoveCategoryRequest : DlpArgument
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
