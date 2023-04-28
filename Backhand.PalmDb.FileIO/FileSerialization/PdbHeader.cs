using System;
using System.IO;
using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.PalmDb.FileIO.FileSerialization
{
    [GenerateBinarySerialization]
    public partial class PdbHeader : IBinarySerializable
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
        private PalmDbDateTime CreationPdbDate { get; } = new();

        [BinarySerialize]
        private PalmDbDateTime ModificationPdbDate { get; } = new();

        [BinarySerialize]
        private PalmDbDateTime LastBackupPdbDate { get; } = new();

        [BinarySerialize]
        public uint ModificationNumber { get; set; }

        [BinarySerialize]
        public uint AppInfoId { get; set; }

        [BinarySerialize]
        public uint SortInfoId { get; set; }

        [BinarySerialize]
        private FixedSizeBinaryString TypeString { get; } = new(4);

        [BinarySerialize]
        private FixedSizeBinaryString CreatorString { get; } = new(4);

        [BinarySerialize]
        public uint UniqueIdSeed { get; set; }

        public string Name
        {
            get => NameString;
            set => NameString.Value = value;
        }

        public string Type
        {
            get => TypeString;
            set => TypeString.Value = value;
        }

        public string Creator
        {
            get => CreatorString;
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

        public PdbHeader()
        {
        }

        public PdbHeader(PalmDbHeader sourceHeader, uint appInfoId, uint sortInfoId)
        {
            Name = sourceHeader.Name;
            Attributes = sourceHeader.Attributes;
            Version = sourceHeader.Version;
            CreationDate = sourceHeader.CreationDate;
            ModificationDate = sourceHeader.ModificationDate;
            LastBackupDate = sourceHeader.LastBackupDate;
            ModificationNumber = sourceHeader.ModificationNumber;
            AppInfoId = appInfoId;
            SortInfoId = sortInfoId;
            Type = sourceHeader.Type;
            Creator = sourceHeader.Creator;
            UniqueIdSeed = sourceHeader.UniqueIdSeed;
        }

        public PalmDbHeader ToPalmDbHeader()
        {
            return new()
            {
                Name = Name,
                Attributes = Attributes,
                Version = Version,
                CreationDate = CreationDate,
                ModificationDate = ModificationDate,
                LastBackupDate = LastBackupDate,
                ModificationNumber = ModificationNumber,
                Type = Type,
                Creator = Creator,
                UniqueIdSeed = UniqueIdSeed
            };
        }

        public PalmDbFileHeader ToPalmDbFileHeader(FileInfo file)
        {
            return (PalmDbFileHeader)ToPalmDbHeader() with
            {
                File = file
            };
        }
    }
}
