using Backhand.Common.BinarySerialization;
using System;

namespace Backhand.Pdb.FileSerialization
{
    [BinarySerializable]
    internal class PdbHeader
    {
        [BinarySerialize(Length = 32, NullTerminated = true)]
        public string Name { get; set; } = string.Empty;

        [BinarySerialize]
        public DatabaseAttributes Attributes { get; set; }

        [BinarySerialize]
        public ushort Version { get; set; }

        [BinarySerialize]
        public uint CreationDateUInt32 { get; set; }

        [BinarySerialize]
        public uint ModificationDateUInt32 { get; set; }

        [BinarySerialize]
        public uint LastBackupDateUInt32 { get; set; }

        [BinarySerialize]
        public uint ModificationNumber { get; set; }

        [BinarySerialize]
        public uint AppInfoId { get; set; }

        [BinarySerialize]
        public uint SortInfoId { get; set; }

        [BinarySerialize(Length = 4)]
        public string Type { get; set; } = string.Empty;

        [BinarySerialize(Length = 4)]
        public string Creator { get; set; } = string.Empty;

        [BinarySerialize]
        public uint UniqueIdSeed { get; set; }

        public DateTime CreationDate
        {
            get => PdbPrimitives.FromPdbDateTime(CreationDateUInt32);
            set => CreationDateUInt32 = PdbPrimitives.ToPdbDateTime(value);
        }

        public DateTime ModificationDate
        {
            get => PdbPrimitives.FromPdbDateTime(ModificationDateUInt32);
            set => ModificationDateUInt32 = PdbPrimitives.ToPdbDateTime(value);
        }

        public DateTime LastBackupDate
        {
            get => PdbPrimitives.FromPdbDateTime(LastBackupDateUInt32);
            set => LastBackupDateUInt32 = PdbPrimitives.ToPdbDateTime(value);
        }
    }
}
