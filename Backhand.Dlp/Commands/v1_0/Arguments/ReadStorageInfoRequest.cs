using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadStorageInfoRequest : IBinarySerializable
    {
        [BinarySerialize] public byte CardNo { get; set; }
        [BinarySerialize] private byte Padding { get; set; } = 0;
    }
}
