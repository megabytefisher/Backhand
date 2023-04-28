using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;
using Backhand.Dlp.Commands.v1_0.Data;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class CreateDbRequest : IBinarySerializable
    {
        [BinarySerialize] private FixedSizeBinaryString CreatorString { get; } = new(4);
        [BinarySerialize] private FixedSizeBinaryString TypeString { get; } = new(4);
        [BinarySerialize] public byte CardId { get; set; }
        [BinarySerialize] private byte Padding { get; set; } = 0;
        [BinarySerialize] public DlpDatabaseAttributes Attributes { get; set; }
        [BinarySerialize] public ushort Version { get; set; }
        [BinarySerialize] private NullTerminatedBinaryString NameString { get; } = new();

        public string Creator
        {
            get => CreatorString.Value;
            set => CreatorString.Value = value;
        }

        public string Type
        {
            get => TypeString.Value;
            set => TypeString.Value = value;
        }

        public string Name
        {
            get => NameString.Value;
            set => NameString.Value = value;
        }
    }
}
