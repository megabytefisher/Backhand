using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;
using System;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class CallApplicationRequest : DlpArgument
    {
        [BinarySerialize]
        private FixedSizeBinaryString CreatorString { get; } = new(4);

        [BinarySerialize]
        public ushort Action { get; set; }

        [BinarySerialize]
        public ushort DataLength
        {
            get => (ushort)Data.Length;
            set => Data = new byte[value];
        }

        [BinarySerialize]
        public byte[] Data { get; private set; } = Array.Empty<byte>();

        public string Creator
        {
            get => CreatorString.Value;
            set => CreatorString.Value = value;
        }
    }
}
