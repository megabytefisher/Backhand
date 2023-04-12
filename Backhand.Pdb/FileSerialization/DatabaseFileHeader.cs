using Backhand.Common.BinarySerialization;

namespace Backhand.Pdb.FileSerialization
{
    internal class DatabaseFileHeader
    {
        [BinarySerialized]
        public string Name { get; set; } = string.Empty;

        [BinarySerialized]
        public DatabaseAttributes Attributes { get; set; }

        [BinarySerialized]
        public ushort Version { get; set; }

        [BinarySerialized]
        public uint CreationDateUInt32 { get; set; }

        [BinarySerialized]
        public uint ModificationDateUInt32 { get; set; }

        [BinarySerialized]
        public uint LastBackupDateUInt32 { get; set; }

        [BinarySerialized]
        public uint ModificationNumber { get; set; }

        [BinarySerialized]
        public uint AppInfoId { get; set; }

        [BinarySerialized]
        public uint SortInfoId { get; set; }

        [BinarySerialized]
        public string Type { get; set; } = string.Empty;

        [BinarySerialized]
        public string Creator { get; set; } = string.Empty;

        [BinarySerialized]
        public uint UniqueIdSeed { get; set; }

        public DateTime CreationDate
        {
            get => PdbSerialization.FromPdbDateTime(CreationDateUInt32);
            set => CreationDateUInt32 = PdbSerialization.ToPdbDateTime(value);
        }

        public DateTime ModificationDate
        {
            get => PdbSerialization.FromPdbDateTime(ModificationDateUInt32);
            set => ModificationDateUInt32 = PdbSerialization.ToPdbDateTime(value);
        }

        public DateTime LastBackupDate
        {
            get => PdbSerialization.FromPdbDateTime(LastBackupDateUInt32);
            set => LastBackupDateUInt32 = PdbSerialization.ToPdbDateTime(value);
        }
    }
}
