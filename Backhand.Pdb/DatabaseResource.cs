using Backhand.Common.BinarySerialization;
using Backhand.Common.Buffers;
using Backhand.Pdb.FileSerialization;
using System.Buffers;

namespace Backhand.Pdb
{
    public class DatabaseResource
    {
        public string Type { get; set; } = string.Empty;
        public ushort ResourceId { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
