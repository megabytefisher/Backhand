using System;

namespace Backhand.Pdb
{
    public class DatabaseResource
    {
        public string Type { get; set; } = "";
        public ushort ResourceId { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
