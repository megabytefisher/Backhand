using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadRecordByIdRequest : IBinarySerializable
    {
        [BinarySerialize] public byte DbHandle { get; set; }
        [BinarySerialize] private byte Padding { get; set; }
        [BinarySerialize] public uint RecordId { get; set; }
        [BinarySerialize] public ushort Offset { get; set; }
        [BinarySerialize] public ushort MaxLength { get; set; }
    }
}