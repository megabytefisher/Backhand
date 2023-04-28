using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.PalmDb
{
    public interface IPalmDbRepository
    {
        Task<ICollection<PalmDbHeader>> GetHeadersAsync(CancellationToken cancellationToken = default);
        Task<IPalmDb> OpenDatabaseAsync(PalmDbHeader header, CancellationToken cancellationToken = default);
        Task<IPalmDb> CreateDatabaseAsync(PalmDbHeader header, CancellationToken cancellationToken = default);
        Task DeleteDatabaseAsync(PalmDbHeader header, CancellationToken cancellationToken = default);
        Task CloseDatabaseAsync(IPalmDb database, CancellationToken cancellationToken = default);
    }
}