using System;
using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadNextModifiedRecordResponse : DlpArgument
    {
        [BinarySerialize]
        public uint RecordId { get; set; }
        
        [BinarySerialize]
        public ushort Index { get; set; }

        [BinarySerialize]
        public ushort Length
        {
            get => (ushort)Data.Length;
            set => Data = new byte[value];
        }

        [BinarySerialize]
        public DlpRecordAttributes Attributes { get; set; }

        [BinarySerialize]
        public byte Category { get; set; }

        [BinarySerialize]
        public byte[] Data { get; private set; } = Array.Empty<byte>();
    }
}