using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class DeleteResourceRequest : IBinarySerializable
    {
        [BinarySerialize] public byte DbHandle { get; set; }
        [BinarySerialize] public byte Flags { get; set; }
        [BinarySerialize] public FixedSizeBinaryString TypeString { get; } = new(4);
        [BinarySerialize] public ushort ResourceId { get; set; }

        public string Type
        {
            get => TypeString.Value;
            set => TypeString.Value = value;
        }
    }
}
