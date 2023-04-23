using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadResourceByIndexRequest : IBinarySerializable
    {
        [BinarySerialize]
        public byte DbHandle { get; set; }

        [BinarySerialize]
        public byte Padding { get; set; }

        [BinarySerialize]
        public ushort ResourceIndex { get; set; }

        [BinarySerialize]
        public ushort Offset { get; set; }

        [BinarySerialize]
        public ushort MaxLength { get; set; }
    }
}