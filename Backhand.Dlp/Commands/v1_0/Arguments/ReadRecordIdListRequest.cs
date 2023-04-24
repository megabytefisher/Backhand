using System;
using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadRecordIdListRequest : IBinarySerializable
    {
        [Flags]
        public enum ReadRecordIdListFlags : byte
        {
            None            = 0b00000000,
            ShouldSort      = 0b10000000
        };

        [BinarySerialize]
        public byte DbHandle { get; set; }

        [BinarySerialize]
        public ReadRecordIdListFlags Flags { get; set; }

        [BinarySerialize]
        public ushort StartIndex { get; set; }

        [BinarySerialize]
        public ushort MaxRecords { get; set; }
    }
}