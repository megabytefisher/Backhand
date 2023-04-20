using System;
using Backhand.Pdb;

namespace Backhand.Cli.Internal.DatabaseDisassembly
{
    public class DatabaseHeaderInfo
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
    }
}