using Backhand.Pdb;

namespace Backhand.Cli.Internal.DatabaseDisassembly
{
    public class DatabaseRecordInfo
    {
        public DatabaseRecordAttributes Attributes { get; set; }
        public byte Category { get; set; }
        public bool Archive { get; set; }
        public uint UniqueId { get; set; }
    }
}