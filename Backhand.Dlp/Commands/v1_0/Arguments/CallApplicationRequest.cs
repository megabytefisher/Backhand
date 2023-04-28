using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;
using System;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class CallApplicationRequest : IBinarySerializable
    {
        [BinarySerialize] private FixedSizeBinaryString CreatorString { get; } = new(4);
        [BinarySerialize] public ushort Action { get; set; }
        [BinarySerialize] public ushort DataLength { get => Convert.ToUInt16(Data.Length); set => Data = new byte[value]; }
        [BinarySerialize] public byte[] Data { get; set; } = Array.Empty<byte>();

        public string Creator
        {
            get => CreatorString;
            set => CreatorString.Value = value;
        }
    }
}