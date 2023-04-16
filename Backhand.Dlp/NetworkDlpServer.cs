using Backhand.Network;
using Backhand.Protocols.Dlp;
using Backhand.Protocols.NetSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public class NetworkDlpServer : DlpServer
    {
        public IPAddress BindAddress { get; set; } = IPAddress.Any;

        public NetworkDlpServer(DlpSyncFunc syncFunc) : base(syncFunc)
        {
        }

        public override async Task RunAsync(bool singleSync = false, CancellationToken cancellationToken = default)
        {
            TcpListener tcpListener = new(new IPEndPoint(BindAddress, 14238));

            List<NetworkDlpClient> clients = new();

            using CancellationTokenSource innerCts = new();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCts.Token);

            //using NetworkHandshakeServer handshakeServer = new(14237);
            //Task handshakeServerTask = handshakeServer.Run(linkedCts.Token);

            try
            {
                tcpListener.Start();
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    clients.RemoveAll(c => c.HandlerTask.IsCompleted);
                    TcpClient client = await tcpListener.AcceptTcpClientAsync(linkedCts.Token).ConfigureAwait(false);
                    NetworkDlpClient networkDlpClient = new(client, DoSyncAsync, linkedCts.Token);
                    clients.Add(networkDlpClient);

                    if (singleSync)
                    {
                        break;
                    }
                }
                tcpListener.Stop();
                await Task.WhenAll(clients.Select(c => c.HandlerTask)).ConfigureAwait(false);
            }
            finally
            {
                tcpListener.Stop();
                innerCts.Cancel();
                try
                {
                    await Task.WhenAll(clients.Select(c => c.HandlerTask)).ConfigureAwait(false);
                }
                catch { /* Swallow */ }

                foreach (NetworkDlpClient client in clients)
                {
                    client.Dispose();
                }
            }
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
