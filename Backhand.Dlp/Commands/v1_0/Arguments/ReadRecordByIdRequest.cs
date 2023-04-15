using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadRecordByIdRequest : DlpArgument
    {
        [BinarySerialize]
        public byte DbHandle { get; set; }

        [BinarySerialize]
        public byte Padding { get; set; }

        [BinarySerialize]
        public uint RecordId { get; set; }

        [BinarySerialize]
        public ushort Offset { get; set; }

        [BinarySerialize]
        public ushort MaxLength { get; set; }
    }
}