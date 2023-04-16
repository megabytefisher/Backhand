using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public class AggregatedDlpServer : IDlpServer
    {
        private readonly IDlpServer[] _servers;

        public event EventHandler<DlpSyncStartingEventArgs>? SyncStarting
        {
            add => Array.ForEach(_servers, s => s.SyncStarting += value);
            remove => Array.ForEach(_servers, s => s.SyncStarting -= value);
        }
        public event EventHandler<DlpSyncEndedEventArgs>? SyncEnded
        {
            add => Array.ForEach(_servers, s => s.SyncEnded += value);
            remove => Array.ForEach(_servers, s => s.SyncEnded -= value);
        }

        public AggregatedDlpServer(IEnumerable<IDlpServer> servers)
        {
            _servers = servers.ToArray();
        }

        public AggregatedDlpServer(params IDlpServer[] servers)
        {
            _servers = servers;
        }

        public Task RunAsync(bool singleSync = false, CancellationToken cancellationToken = default)
        {
            return Task.WhenAll(Array.ConvertAll(_servers, s => s.RunAsync(singleSync, cancellationToken)));
        }
    }
}