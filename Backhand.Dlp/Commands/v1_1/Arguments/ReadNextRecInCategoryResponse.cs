using System;
using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;
using Backhand.Dlp.Commands.v1_0.Data;

namespace Backhand.Dlp.Commands.v1_1.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadNextRecInCategoryResponse : IBinarySerializable
    {
        [BinarySerialize] public uint RecordId { get; set; }
        [BinarySerialize] public ushort RecordIndex { get; set; }
        [BinarySerialize] public ushort RecordSize { get => Convert.ToUInt16(RecordData.Length); set => RecordData = new byte[value]; }
        [BinarySerialize] public DlpRecordAttributes Attributes { get; set; }
        [BinarySerialize] public byte Category { get; set; }
        [BinarySerialize] public byte[] RecordData { get; set; } = Array.Empty<byte>();
    }
}