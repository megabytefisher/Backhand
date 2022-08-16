using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public interface IDlpServer
    {
        event EventHandler<DlpSyncStartingEventArgs>? SyncStarting;
        event EventHandler<DlpSyncEndedEventArgs>? SyncEnded;

        Task RunAsync(CancellationToken cancellationToken);
    }
}
