using Backhand.Common.BinarySerialization;
using System;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class OpenDbRequest : IBinarySerializable
    {
        [Flags]
        public enum OpenDbMode : byte
        {
            Read            = 0b10000000,
            Write           = 0b01000000,
            Exclusive       = 0b00100000,
            Secret          = 0b00010000,
        }

        [BinarySerialize] public byte CardId { get; set; }
        [BinarySerialize] public OpenDbMode Mode { get; set; }
        [BinarySerialize] private NullTerminatedBinaryString NameString { get; } = new NullTerminatedBinaryString();

        public string Name
        {
            get => NameString.Value;
            set => NameString.Value = value;
        }
    }
}
