using Backhand.Common.BinarySerialization;
using System;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Pdb.FileSerialization
{
    [GenerateBinarySerialization]
    internal partial class PdbHeader : IBinarySerializable
    {
        [BinarySerialize]
        private FixedSizeBinaryString NameString { get; set; } = new(32)
        {
            NullTerminated = true
        };

        [BinarySerialize]
        public DatabaseAttributes Attributes { get; set; }

        [BinarySerialize]
        public ushort Version { get; set; }

        [BinarySerialize]
        private PdbDateTime CreationPdbDate { get; } = new();

        [BinarySerialize]
        private PdbDateTime ModificationPdbDate { get; } = new();

        [BinarySerialize]
        private PdbDateTime LastBackupPdbDate { get; } = new();

        [BinarySerialize]
        public uint ModificationNumber { get; set; }

        [BinarySerialize]
        public uint AppInfoId { get; set; }

        [BinarySerialize]
        public uint SortInfoId { get; set; }

        [BinarySerialize]
        private FixedSizeBinaryString TypeString { get; set; } = new(4);

        [BinarySerialize]
        private FixedSizeBinaryString CreatorString { get; set; } = new(4);

        [BinarySerialize]
        public uint UniqueIdSeed { get; set; }

        public string Name
        {
            get => NameString.ToString();
            set => NameString.Value = value;
        }

        public string Type
        {
            get => TypeString.ToString();
            set => TypeString.Value = value;
        }

        public string Creator
        {
            get => CreatorString.ToString();
            set => CreatorString.Value = value;
        }

        public DateTime CreationDate
        {
            get => CreationPdbDate;
            set => CreationPdbDate.AsDateTime = value;
        }

        public DateTime ModificationDate
        {
            get => ModificationPdbDate;
            set => ModificationPdbDate.AsDateTime = value;
        }

        public DateTime LastBackupDate
        {
            get => LastBackupPdbDate;
            set => LastBackupPdbDate.AsDateTime = value;
        }
    }
}
