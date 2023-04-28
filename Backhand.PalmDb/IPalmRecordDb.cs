using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.PalmDb
{
    public interface IPalmRecordDb : IPalmDb
    {
        Task<ICollection<uint>> GetRecordIdsAsync(CancellationToken cancellationToken = default);
        Task<PalmDbRecordHeader?> ReadRecordByIndexAsync(ushort index, Stream? dataStream, CancellationToken cancellationToken = default);
        Task<PalmDbRecordHeader> ReadRecordByIdAsync(uint id, Stream? dataStream, CancellationToken cancellationToken = default);
        Task WriteRecordAsync(PalmDbRecordHeader header, System.Memory<byte> data, CancellationToken cancellationToken = default);
    }
}