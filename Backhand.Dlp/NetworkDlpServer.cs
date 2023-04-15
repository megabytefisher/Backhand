using Backhand.Network;
using Backhand.Protocols.Dlp;
using Backhand.Protocols.NetSync;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public class NetworkDlpServer : DlpServer
    {
        private readonly TcpListener _listener;
        private readonly List<NetworkDlpClient> _clients = new List<NetworkDlpClient>();

        public NetworkDlpServer(DlpSyncFunc syncFunc, int port = 14238, CancellationToken cancellationToken = default) : base(syncFunc)
        {
            _listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
        }

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {

            using CancellationTokenSource innerCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCts.Token);

            using NetworkHandshakeServer handshakeServer = new(14237);
            Task handshakeServerTask = handshakeServer.Run(linkedCts.Token);

            try
            {
                _listener.Start();
                while (true)
                {
                    RemoveDeadClients();
                    cancellationToken.ThrowIfCancellationRequested();
                    TcpClient client = await _listener.AcceptTcpClientAsync(linkedCts.Token).ConfigureAwait(false);
                    NetworkDlpClient networkDlpClient = new(client, DoSyncAsync, linkedCts.Token);
                    _clients.Add(networkDlpClient);
                }
            }
            finally
            {
                innerCts.Cancel();
                try
                {
                    await handshakeServerTask;
                }
                catch
                {
                    // Swallow
                }
            }
        }

        private void RemoveDeadClients()
        {
            _clients.RemoveAll(c => c.HandlerTask.IsCompleted);
        }

        private class NetworkDlpClient : IDisposable
        {
            public TcpClient Client { get; }
            public DlpSyncFunc SyncFunc { get; }
            public Task HandlerTask { get; }
            public CancellationTokenSource CancellationTokenSource { get; set; }

            private CancellationToken _externalCancellationToken;

            public NetworkDlpClient(TcpClient client, DlpSyncFunc syncFunc, CancellationToken cancellationToken)
            {
                Client = client;
                SyncFunc = syncFunc;
                CancellationTokenSource = new CancellationTokenSource();
                _externalCancellationToken = cancellationToken;

                HandlerTask = Task.Run(HandleDeviceAsync);
            }

            public void Dispose()
            {
                Client.Dispose();
                CancellationTokenSource.Dispose();
            }

            private async Task HandleDeviceAsync()
            {
                using CancellationTokenSource innerCts = new();
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_externalCancellationToken, CancellationTokenSource.Token, innerCts.Token);

                NetworkPipe pipe = new(Client);

                using NetSyncInterface netSyncInterface = new(pipe);
                NetSyncConnection netSyncConnection = new(netSyncInterface);

                Task? netSyncHandshakeTask = null;
                Task? netSyncIoTask = null;
                List<Task> tasks = new List<Task>(3);
                try
                {
                    // Start the handshake task early enough to see the wakeup packet
                    netSyncHandshakeTask = netSyncConnection.DoHandshakeAsync(linkedCts.Token);
                    tasks.Add(netSyncHandshakeTask);

                    // Start IO tasks
                    netSyncIoTask = netSyncInterface.RunIOAsync(linkedCts.Token);
                    tasks.Add(netSyncIoTask);

                    // Wait for the handshake task to complete
                    await netSyncHandshakeTask.ConfigureAwait(false);

                    // Build up the DLP connection
                    DlpConnection dlpConnection = new(netSyncConnection);
                    await SyncFunc(dlpConnection, linkedCts.Token);
                }
                finally
                {
                    innerCts.Cancel();
                    try
                    {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Swallow.
                    }
                }
            }
        }
    }
}
