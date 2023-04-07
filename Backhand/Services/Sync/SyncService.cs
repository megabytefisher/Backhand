using System.Threading;
using System.Threading.Tasks;
using Backhand.DeviceIO.DlpServers;

namespace Backhand.Services.Sync
{
    public class SyncService
    {
        private IDlpServer? _server;
        private CancellationTokenSource? _serverCts;
        private Task? _serverTask;
    
        public SyncService()
        {
        }

        public void Start()
        {
            _server = new UsbDlpServer(DoSyncAsync);
            _serverCts = new CancellationTokenSource();
            _serverTask = _server.RunAsync(_serverCts.Token);
        }

        private Task DoSyncAsync(DlpClientContext context, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}