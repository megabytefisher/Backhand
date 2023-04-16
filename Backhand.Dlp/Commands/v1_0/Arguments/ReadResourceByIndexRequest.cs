using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    public class ReadResourceByIndexRequest : DlpArgument
    {
        [BinarySerialize]
        public byte DbHandle { get; set; }

        [BinarySerialize]
        public ushort ResourceIndex { get; set; }

        [BinarySerialize]
        public ushort Offset { get; set; }

        [BinarySerialize]
        public ushort MaxLength { get; set; }
    }
}