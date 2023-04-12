using Backhand.Common.BinarySerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb.FileSerialization
{
    internal class DatabaseFileResourceMetadata
    {
        [BinarySerialized]
        public string Type { get; set; } = string.Empty;

        [BinarySerialized]
        public ushort ResourceId { get; set; }

        [BinarySerialized]
        public uint LocalChunkId { get; set; }
    }
}
