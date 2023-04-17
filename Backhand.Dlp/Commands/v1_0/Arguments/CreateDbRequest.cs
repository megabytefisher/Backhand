﻿using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class CreateDbRequest : DlpArgument
    {
        [BinarySerialize]
        private FixedSizeBinaryString CreatorString { get; } = new(4);

        [BinarySerialize]
        private FixedSizeBinaryString TypeString { get; } = new(4);

        [BinarySerialize]
        public byte CardId { get; set; }

        [BinarySerialize]
        public byte Padding { get; set; } = 0;

        [BinarySerialize]
        public DlpDatabaseAttributes Attributes { get; set; }

        [BinarySerialize]
        public ushort Version { get; set; }

        [BinarySerialize]
        private NullTerminatedBinaryString NameString { get; } = new();

        public string Creator
        {
            get => CreatorString.Value;
            set => CreatorString.Value = value;
        }

        public string Type
        {
            get => TypeString.Value;
            set => TypeString.Value = value;
        }

        public string Name
        {
            get => NameString.Value;
            set => NameString.Value = value;
        }
    }
}
