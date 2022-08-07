using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public class DatabaseRecord
    {
        public RecordAttributes Attributes { get; set; }
        public uint UniqueId { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
