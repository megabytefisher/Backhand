using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public interface IDlpServer
    {
        event EventHandler<DlpSyncStartingEventArgs>? SyncStarting;
        event EventHandler<DlpSyncEndedEventArgs>? SyncEnded;

        Task RunAsync(bool singleSync = false, CancellationToken cancellationToken = default);
    }
}
