using Backhand.PalmDb;

namespace Backhand.Cli.Internal.DatabaseDisassembly
{
    public class RecordManifest
    {
        public uint Id { get; set; }
        public DatabaseRecordAttributes Attributes { get; set; }
        public byte Category { get; set; }
        public bool Archive { get; set; }
        public string Path { get; set; } = string.Empty;
        
        public RecordManifest()
        {
        }
        
        public RecordManifest(PalmDbRecordHeader recordHeader, string path)
        {
            Id = recordHeader.Id;
            Attributes = recordHeader.Attributes;
            Category = recordHeader.Category;
            Archive = recordHeader.Archive;
            Path = path;
        }
    }
}