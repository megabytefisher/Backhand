using System;
using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;
using Backhand.Dlp.Commands.v1_0.Data;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadRecordResponse : IBinarySerializable
    {
        [BinarySerialize] public uint RecordId { get; set; }
        [BinarySerialize] public ushort Index { get; set; }
        [BinarySerialize] public ushort Length { get => Convert.ToUInt16(Data.Length); set => Data = new byte[value]; }
        [BinarySerialize] public DlpRecordAttributes Attributes { get; set; }
        [BinarySerialize] public byte Category { get; set; }
        [BinarySerialize] public byte[] Data { get; private set; } = Array.Empty<byte>();
    }
}