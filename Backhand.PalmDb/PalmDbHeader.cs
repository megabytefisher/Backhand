using System;

namespace Backhand.PalmDb
{
    public record PalmDbHeader
    {
        public required string Name { get; init; }
        public required DatabaseAttributes Attributes { get; init; }
        public required ushort Version { get; init; }
        public required DateTime CreationDate { get; init; }
        public required DateTime ModificationDate { get; init; }
        public required DateTime LastBackupDate { get; init; }
        public required uint ModificationNumber { get; init; }
        public required string Type { get; init; }
        public required string Creator { get; init; }
        public required uint UniqueIdSeed { get; init; }
    }
}