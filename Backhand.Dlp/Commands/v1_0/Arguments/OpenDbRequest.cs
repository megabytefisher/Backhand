using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;
using System;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class OpenDbRequest : DlpArgument
    {
        [Flags]
        public enum OpenDbMode : byte
        {
            Read            = 0b10000000,
            Write           = 0b01000000,
            Exclusive       = 0b00100000,
            Secret          = 0b00010000,
        }

        [BinarySerialize]
        public byte CardId { get; set; }

        [BinarySerialize]
        public OpenDbMode Mode { get; set; }

        [BinarySerialize(NullTerminated = true)]
        public string Name { get; set; } = string.Empty;
    }
}
