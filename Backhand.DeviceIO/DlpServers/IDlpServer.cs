using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public interface IDlpServer
    {
        event EventHandler<DlpSyncStartingEventArgs>? SyncStarting;
        event EventHandler<DlpSyncEndedEventArgs>? SyncEnded;

        Task Run(CancellationToken cancellationToken);
    }
}
