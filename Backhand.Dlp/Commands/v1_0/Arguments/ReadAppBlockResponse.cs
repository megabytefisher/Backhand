using Backhand.Common.BinarySerialization;
using System;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadAppBlockResponse : IBinarySerializable
    {
        [BinarySerialize] public ushort Size { get => Convert.ToUInt16(Data.Length); set => Data = new byte[value]; }
        [BinarySerialize] public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
