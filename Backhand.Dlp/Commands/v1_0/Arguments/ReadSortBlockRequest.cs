using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadSortBlockRequest : DlpArgument
    {
        [BinarySerialize]
        public byte DbHandle { get; set; }

        [BinarySerialize]
        public byte Padding { get; set; } = 0;

        [BinarySerialize]
        public ushort Offset { get; set; }

        [BinarySerialize]
        public ushort Length { get; set; }
    }
}
