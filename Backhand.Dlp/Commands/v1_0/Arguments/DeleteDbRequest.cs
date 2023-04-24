using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class DeleteDbRequest : IBinarySerializable
    {
        [BinarySerialize]
        public byte CardId { get; set; }

        [BinarySerialize]
        public byte Padding { get; set; } = 0;

        [BinarySerialize]
        private NullTerminatedBinaryString NameString { get; } = new();

        public string Name
        {
            get => NameString.Value;
            set => NameString.Value = value;
        }
    }
}
