using System;
using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class WriteAppBlockRequest : DlpArgument
    {
        [BinarySerialize]
        public byte DbHandle { get; set; }

        [BinarySerialize]
        public byte Padding { get; set; } = 0;

        [BinarySerialize]
        public ushort Length
        {
            get => (ushort)Data.Length;
            set => Data = new byte[value];
        }

        [BinarySerialize]
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}