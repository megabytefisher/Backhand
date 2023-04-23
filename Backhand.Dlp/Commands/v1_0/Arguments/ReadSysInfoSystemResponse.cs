using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;
using System;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadSysInfoSystemResponse : IBinarySerializable
    {
        [BinarySerialize]
        public uint RomVersion { get; set; }

        [BinarySerialize]
        public uint Locale { get; set; }

        [BinarySerialize]
        public byte ProductIdLength
        {
            get => (byte)ProductId.Length;
            set => ProductId = new byte[value];
        }

        [BinarySerialize]
        public byte[] ProductId { get; set; } = Array.Empty<byte>();
    }
}
