using System;
using Backhand.PalmDb;

namespace Backhand.Cli.Internal.DatabaseDisassembly
{
    public class PalmDbManifest
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
        public string AppInfoPath { get; set; }
        public string SortInfoPath { get; set; }

        public PalmDbManifest()
        {
        }

        public PalmDbManifest(PalmDbHeader dbHeader, string appInfoPath, string sortInfoPath)
        {
            Name = dbHeader.Name;
            Attributes = dbHeader.Attributes;
            Version = dbHeader.Version;
            CreationDate = dbHeader.CreationDate;
            ModificationDate = dbHeader.ModificationDate;
            LastBackupDate = dbHeader.LastBackupDate;
            ModificationNumber = dbHeader.ModificationNumber;
            Type = dbHeader.Type;
            Creator = dbHeader.Creator;
            UniqueIdSeed = dbHeader.UniqueIdSeed;
            AppInfoPath = appInfoPath;
            SortInfoPath = sortInfoPath;
        }
    }
}