using Backhand.Common.BinarySerialization;
using System;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadSysInfoResponse : IBinarySerializable
    {
        [BinarySerialize] public uint RomVersion { get; set; }
        [BinarySerialize] public uint Locale { get; set; }
        [BinarySerialize] public byte ProductIdLength { get => Convert.ToByte(ProductId.Length); set => ProductId = new byte[value]; }
        [BinarySerialize] public byte[] ProductId { get; set; } = Array.Empty<byte>();
    }
}
