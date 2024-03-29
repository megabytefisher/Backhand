using System;
using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadRecordIdListResponse : IBinarySerializable
    {
        [BinarySerialize] public ushort Count { get => Convert.ToUInt16(RecordIds.Length); set => RecordIds = new uint[value]; }
        [BinarySerialize] public uint[] RecordIds { get; set; } = Array.Empty<uint>();
    }
}