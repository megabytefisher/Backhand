using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class DeleteRecordRequest : IBinarySerializable
    {
        [BinarySerialize] public byte DbHandle { get; set; }
        [BinarySerialize] public byte Flags { get; set; }
        [BinarySerialize] public uint RecordId { get; set; }
    }
}
