using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public interface IDlpServer<TContext>
    {
        event EventHandler<DlpSyncStartingEventArgs<TContext>>? SyncStarting;
        event EventHandler<DlpSyncEndedEventArgs<TContext>>? SyncEnded;

        Task RunAsync(bool singleSync = false, CancellationToken cancellationToken = default);
    }
}
