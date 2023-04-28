using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ResetSyncFlagsRequest : IBinarySerializable
    {
        [BinarySerialize] public byte DbHandle { get; set; }
    }
}
