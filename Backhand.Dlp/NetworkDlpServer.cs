using Backhand.Network;
using Backhand.Protocols.Dlp;
using Backhand.Protocols.NetSync;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public class NetworkDlpServer<TContext> : DlpServer<TContext>
    {
        public IPEndPoint NetworkEndPoint { get; }

        private const int DefaultHandshakePort = 14237;
        private const int DefaultMainPort = 14238;

        public NetworkDlpServer(IPEndPoint networkEndPoint, ILoggerFactory? loggerFactory = null)
            : base(loggerFactory)
        {
            NetworkEndPoint = networkEndPoint;
        }

        public NetworkDlpServer(IPAddress bindAddress, ILoggerFactory? loggerFactory = null)
            : this(new IPEndPoint(bindAddress, DefaultMainPort), loggerFactory)
        {
        }

        public NetworkDlpServer(ILoggerFactory? loggerFactory = null)
            : this(IPAddress.Any, loggerFactory)
        {
        }

        public override async Task RunAsync(ISyncHandler<TContext> context, CancellationToken cancellationToken)
        {
            await RunAsync(context, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task RunAsync(ISyncHandler<TContext> syncHandler, bool singleSync, CancellationToken cancellationToken = default)
        {
            TcpListener tcpListener = new(NetworkEndPoint);

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
                    NetworkDlpClient networkDlpClient = new(client, (connection, cancellation) => HandleConnection(connection, syncHandler, cancellationToken), linkedCts.Token, LoggerFactory);
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
            public Func<DlpConnection, CancellationToken, Task> SyncFunc { get; }
            public Task HandlerTask { get; }
            public CancellationTokenSource CancellationTokenSource { get; set; }

            private CancellationToken _externalCancellationToken;

            private readonly ILogger _netSyncInterfaceLogger;
            private readonly ILogger _netSyncConnectionLogger;
            private readonly ILogger _dlpConnectionLogger;

            public NetworkDlpClient(TcpClient client, Func<DlpConnection, CancellationToken, Task> syncFunc, CancellationToken cancellationToken, ILoggerFactory loggerFactory)
            {
                Client = client;
                SyncFunc = syncFunc;
                CancellationTokenSource = new CancellationTokenSource();
                _externalCancellationToken = cancellationToken;

                _netSyncInterfaceLogger = loggerFactory.CreateLogger<NetSyncInterface>();
                _netSyncConnectionLogger = loggerFactory.CreateLogger<NetSyncConnection>();
                _dlpConnectionLogger = loggerFactory.CreateLogger<DlpConnection>();

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

                using NetSyncInterface netSyncInterface = new(pipe, logger: _netSyncInterfaceLogger);
                NetSyncConnection netSyncConnection = new(netSyncInterface, logger: _netSyncConnectionLogger);

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
                    DlpConnection dlpConnection = new(netSyncConnection, logger: _dlpConnectionLogger);
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
