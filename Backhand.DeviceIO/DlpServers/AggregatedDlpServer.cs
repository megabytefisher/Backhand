using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public class AggregatedDlpServer : IDlpServer
    {
        private IDlpServer[] _servers;

        public event EventHandler<DlpSyncStartingEventArgs>? SyncStarting
        {
            add
            {
                foreach (IDlpServer server in _servers)
                {
                    server.SyncStarting += value;
                }
            }
            remove
            {
                foreach (IDlpServer server in _servers)
                {
                    server.SyncStarting -= value;
                }
            }
        }

        public event EventHandler<DlpSyncEndedEventArgs>? SyncEnded
        {
            add
            {
                foreach (IDlpServer server in _servers)
                {
                    server.SyncEnded += value;
                }
            }
            remove
            {
                foreach (IDlpServer server in _servers)
                {
                    server.SyncEnded -= value;
                }
            }
        }

        public AggregatedDlpServer(IEnumerable<IDlpServer> servers)
        {
            _servers = servers.ToArray();
        }

        public AggregatedDlpServer(params IDlpServer[] servers)
        {
            _servers = servers;
        }

        public async Task Run(CancellationToken cancellationToken = default)
        {
            Task[] serverTasks = _servers.Select(s => s.Run(cancellationToken)).ToArray();

            await Task.WhenAll(serverTasks);
        }
    }
}
