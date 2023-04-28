using System.IO;

namespace Backhand.PalmDb.FileIO
{
    public record PalmDbFileHeader : PalmDbHeader
    {
        public required FileInfo File { get; init; }
    }
}