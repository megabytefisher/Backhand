using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;
using System;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class WriteRecordRequest : DlpArgument
    {
        [BinarySerialize]
        public byte DbHandle { get; set; }

        [BinarySerialize]
        public byte Flags { get; set; }

        [BinarySerialize]
        public uint RecordId { get; set; }

        [BinarySerialize]
        public DlpRecordAttributes Attributes { get; set; }

        [BinarySerialize]
        public byte Category { get; set; }

        [BinarySerialize]
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
