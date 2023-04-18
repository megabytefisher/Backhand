using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;
using System;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class CallApplicationResponse : DlpArgument
    {
        [BinarySerialize]
        public ushort Action { get; set; }

        [BinarySerialize]
        public ushort Result { get; set; }

        [BinarySerialize]
        public ushort DataLength
        {
            get => (ushort)Data.Length;
            set => Data = new byte[value];
        }

        [BinarySerialize]
        public byte[] Data { get; private set; } = Array.Empty<byte>();
    }
}
