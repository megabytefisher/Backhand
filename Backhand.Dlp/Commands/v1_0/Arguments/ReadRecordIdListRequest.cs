using System;
using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadRecordIdListRequest : DlpArgument
    {
        [Flags]
        public enum ReadRecordIdListFlags : byte
        {
            None = 0b00000000,
            ShouldSort = 0b10000000
        };

        [BinarySerialize]
        public byte DbHandle { get; set; }

        public ReadRecordIdListFlags Flags { get; set; }

        [BinarySerialize]
        public ushort Index { get; set; }

        [BinarySerialize]
        public ushort MaxRecords { get; set; }
    }
}