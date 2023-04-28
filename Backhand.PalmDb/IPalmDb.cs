using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.PalmDb
{
    public interface IPalmDb
    {
        Task<PalmDbHeader> ReadHeaderAsync(CancellationToken cancellationToken = default);
        Task WriteHeaderAsync(PalmDbHeader header, CancellationToken cancellationToken = default);
        
        Task ReadAppInfoAsync(Stream stream, CancellationToken cancellationToken = default);
        Task WriteAppInfoAsync(System.Memory<byte> data, CancellationToken cancellationToken = default);
        
        Task ReadSortInfoAsync(Stream stream, CancellationToken cancellationToken = default);
        Task WriteSortInfoAsync(System.Memory<byte> data, CancellationToken cancellationToken = default);
    }
}