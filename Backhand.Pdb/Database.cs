using Backhand.Pdb.FileSerialization;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public abstract class Database
    {
        public string Name { get; set; } = string.Empty;
        public DatabaseAttributes Attributes { get; set; }
        public ushort Version { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public DateTime LastBackupDate { get; set; }
        public uint ModificationNumber { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public uint UniqueIdSeed { get; set; }

        public byte[]? AppInfo { get; set; }
        public byte[]? SortInfo { get; set; }

        public abstract Task SerializeAsync(Stream stream, CancellationToken cancellationToken);
        public abstract Task DeserializeAsync(Stream stream, CancellationToken cancellationToken);

        internal PdbHeader GetFileHeader(uint appInfoId, uint sortInfoId)
        {
            PdbHeader result = new PdbHeader();
            result.Name = Name;
            result.Attributes = Attributes;
            result.Version = Version;
            result.CreationDate = CreationDate;
            result.ModificationDate = ModificationDate;
            result.LastBackupDate = LastBackupDate;
            result.ModificationNumber = ModificationNumber;
            result.AppInfoId = appInfoId;
            result.SortInfoId = sortInfoId;
            result.Type = Type;
            result.Creator = Creator;
            result.UniqueIdSeed = UniqueIdSeed;
            return result;
        }

        internal void LoadFileHeader(PdbHeader header)
        {
            Name = header.Name;
            Attributes = header.Attributes;
            Version = header.Version;
            CreationDate = header.CreationDate;
            ModificationDate = header.ModificationDate;
            LastBackupDate = header.LastBackupDate;
            ModificationNumber = header.ModificationNumber;
            Type = header.Type;
            Creator = header.Creator;
            UniqueIdSeed = header.UniqueIdSeed;
        }
    }
}
