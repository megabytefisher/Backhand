using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public class AggregatedDlpServer<TContext> : IDlpServer<TContext>
    {
        private readonly IDlpServer<TContext>[] _servers;

        public event EventHandler<DlpSyncStartingEventArgs<TContext>>? SyncStarting
        {
            add => Array.ForEach(_servers, s => s.SyncStarting += value);
            remove => Array.ForEach(_servers, s => s.SyncStarting -= value);
        }
        public event EventHandler<DlpSyncEndedEventArgs<TContext>>? SyncEnded
        {
            add => Array.ForEach(_servers, s => s.SyncEnded += value);
            remove => Array.ForEach(_servers, s => s.SyncEnded -= value);
        }

        public AggregatedDlpServer(IEnumerable<IDlpServer<TContext>> servers)
        {
            _servers = servers.ToArray();
        }

        public AggregatedDlpServer(params IDlpServer<TContext>[] servers)
        {
            _servers = servers;
        }

        public Task RunAsync(bool singleSync = false, CancellationToken cancellationToken = default)
        {
            return Task.WhenAll(Array.ConvertAll(_servers, s => s.RunAsync(singleSync, cancellationToken)));
        }
    }
}