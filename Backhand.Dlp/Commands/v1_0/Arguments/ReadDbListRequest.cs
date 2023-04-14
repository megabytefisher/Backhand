using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;
using System;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadDbListRequest : DlpArgument
    {
        [Flags]
        public enum ReadDbListMode : byte
        {
            ListRam = 0b10000000,
            ListRom = 0b01000000,
            ListMultiple = 0b00100000,
        }

        [BinarySerialize]
        public ReadDbListMode Mode { get; set; }

        [BinarySerialize]
        public byte CardId { get; set; }

        [BinarySerialize]
        public ushort StartIndex { get; set; }
    }
}
