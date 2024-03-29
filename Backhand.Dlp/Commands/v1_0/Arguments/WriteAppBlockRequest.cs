using System;
using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class WriteAppBlockRequest : IBinarySerializable
    {
        [BinarySerialize] public byte DbHandle { get; set; }
        [BinarySerialize] public byte Padding { get; set; }
        [BinarySerialize] public ushort Length { get => Convert.ToUInt16(Data.Length); set => Data = new byte[value]; }
        [BinarySerialize] public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}