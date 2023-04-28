using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_1.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadNextRecInCategoryRequest : IBinarySerializable
    {
        [BinarySerialize] public byte DbHandle { get; set; }
        [BinarySerialize] public byte Category { get; set; }
    }
}