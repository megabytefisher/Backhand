using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.PalmDb
{
    public interface IPalmResourceDb : IPalmDb
    {
        Task<PalmDbResourceHeader?> ReadResourceByIndexAsync(ushort index, Stream? dataStream, CancellationToken cancellationToken);
        Task WriteResourceAsync(PalmDbResourceHeader header, System.Memory<byte> data, CancellationToken cancellationToken);
    }
}