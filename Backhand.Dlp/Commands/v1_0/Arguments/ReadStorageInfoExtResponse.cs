using System;
using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadStorageInfoExtResponse : DlpArgument
    {
        [BinarySerialize]
        public ushort RomDatabaseCount { get; set; }

        [BinarySerialize]
        public ushort RamDatabaseCount { get; set; }

        [BinarySerialize]
        public uint Reserved1 { get; set; }

        [BinarySerialize]
        public uint Reserved2 { get; set; }

        [BinarySerialize]
        public uint Reserved3 { get; set; }

        [BinarySerialize]
        public uint Reserved4 { get; set; }
    }
}
