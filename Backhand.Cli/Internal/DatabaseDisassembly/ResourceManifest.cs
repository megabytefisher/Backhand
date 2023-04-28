using Backhand.PalmDb;

namespace Backhand.Cli.Internal.DatabaseDisassembly
{
    public class ResourceManifest
    {
        public ushort Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        
        public ResourceManifest()
        {
        }
        
        public ResourceManifest(PalmDbResourceHeader recordHeader, string path)
        {
            Id = recordHeader.Id;
            Type = recordHeader.Type;
            Path = path;
        }
    }
}