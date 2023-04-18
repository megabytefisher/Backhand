using System;

namespace Backhand.Pdb
{
    public class DatabaseRecord
    {
        public DatabaseRecordAttributes Attributes { get; set; }
        public byte Category { get; set; }
        public bool Archive { get; set; }
        public uint UniqueId { get; set; }
        public virtual byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
