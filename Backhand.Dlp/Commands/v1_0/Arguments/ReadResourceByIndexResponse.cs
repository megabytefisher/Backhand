using System;
using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadResourceByIndexResponse : DlpArgument
    {
        [BinarySerialize]
        private FixedSizeBinaryString TypeString { get; } = new(4);

        [BinarySerialize]
        public ushort ResourceId { get; set; }

        [BinarySerialize]
        public ushort Index { get; set; }

        [BinarySerialize]
        public ushort Length
        {
            get => (ushort)Data.Length;
            set => Data = new byte[value];
        }

        [BinarySerialize]
        public byte[] Data { get; private set; } = Array.Empty<byte>();

        public string Type
        {
            get => TypeString.Value;
            set => TypeString.Value = value;
        }
    }
}