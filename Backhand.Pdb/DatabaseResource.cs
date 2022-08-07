using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public class DatabaseResource
    {
        public string Type { get; set; } = "";
        public ushort ResourceId { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
