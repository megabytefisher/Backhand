using Backhand.Common.BinarySerialization;
using System;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class WriteRecordRequest : IBinarySerializable
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
