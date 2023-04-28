using Backhand.Common.BinarySerialization;
using System;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class CallApplicationResponse : IBinarySerializable
    {
        [BinarySerialize] public ushort Action { get; set; }
        [BinarySerialize] public ushort Result { get; set; }
        [BinarySerialize] public ushort DataLength { get => Convert.ToUInt16(Data.Length); set => Data = new byte[value]; }
        [BinarySerialize] public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
