using System;

namespace Backhand.Pdb
{
    public class DatabaseRecord
    {
        public RecordAttributes Attributes { get; set; }
        public byte Category { get; set; }
        public bool Archive { get; set; }
        public uint UniqueId { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
