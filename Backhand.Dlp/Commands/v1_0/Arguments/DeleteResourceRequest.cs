using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class DeleteResourceRequest : DlpArgument
    {
        [BinarySerialize]
        public byte DbHandle { get; set; }

        [BinarySerialize]
        public byte Flags { get; set; }

        [BinarySerialize]
        public FixedSizeBinaryString TypeString { get; } = new(4);

        [BinarySerialize]
        public ushort ResourceId { get; set; }

        public string Type
        {
            get => TypeString.Value;
            set => TypeString.Value = value;
        }
    }
}
