using System;
using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_2.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadDbListRequest : IBinarySerializable
    {
        [Flags]
        public enum ReadDbListMode : byte
        {
            None            = 0b00000000,
            ListRam         = 0b10000000,
            ListRom         = 0b01000000,
            ListMultiple    = 0b00100000,
        }

        [BinarySerialize]
        public ReadDbListMode Mode { get; set; }

        [BinarySerialize]
        public byte CardId { get; set; }

        [BinarySerialize]
        public ushort StartIndex { get; set; }
    }
}
