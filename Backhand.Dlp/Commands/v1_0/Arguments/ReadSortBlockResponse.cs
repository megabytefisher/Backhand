using Backhand.Common.BinarySerialization;
using System;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadSortBlockResponse : IBinarySerializable
    {
        [BinarySerialize]
        public ushort Size
        {
            get => (byte)Data.Length;
            set => Data = new byte[value];
        }

        [BinarySerialize]
        public byte[] Data { get; private set; } = Array.Empty<byte>();
    }
}
