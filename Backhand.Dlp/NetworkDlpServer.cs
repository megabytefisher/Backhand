using Backhand.Common;
using Backhand.Network;
using Backhand.Protocols.Dlp;
using Backhand.Protocols.NetSync;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public class NetworkDlpServer : DlpServer
    {
        public IPEndPoint ServerEndPoint { get; }
        
        private readonly ILogger _netSyncInterfaceLogger;
        private readonly ILogger _netSyncConnectionLogger;
        private readonly ILogger _dlpConnectionLogger;

        private const int DefaultHandshakePort = 14237;
        private const int DefaultMainPort = 14238;

        public NetworkDlpServer(IPEndPoint serverEndPoint, ILoggerFactory? loggerFactory = null)
            : base(loggerFactory)
        {
            ServerEndPoint = serverEndPoint;
            
            _netSyncInterfaceLogger = LoggerFactory.CreateLogger(DlpServerLogging.NetSyncInterfaceCategory);
            _netSyncConnectionLogger = LoggerFactory.CreateLogger(DlpServerLogging.NetSyncConnectionCategory);
            _dlpConnectionLogger = LoggerFactory.CreateLogger(DlpServerLogging.DlpConnectionCategory);
        }

        public NetworkDlpServer(IPAddress bindAddress, ILoggerFactory? loggerFactory = null)
            : this(new IPEndPoint(bindAddress, DefaultMainPort), loggerFactory)
        {
        }

        public NetworkDlpServer(ILoggerFactory? loggerFactory = null)
            : this(IPAddress.Any, loggerFactory)
        {
        }

        public override string ToString() => $"network[{ServerEndPoint}]";

        public override async Task RunAsync(ISyncHandler syncHandler, bool singleSync, CancellationToken cancellationToken = default)
        {
            Logger.ServerStarting(this);
            
            TcpListener tcpListener = new(ServerEndPoint);
            List<NetworkDlpClient> clients = new();
            using CancellationTokenSource innerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Exception? exception = null;
            
            try
            {
                tcpListener.Start();
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ValueTask<TcpClient> acceptTask = tcpListener.AcceptTcpClientAsync(innerCts.Token);

                    List<Task> tasks = new() { acceptTask.AsTask() };
                    tasks.AddRange(clients.Select(c => c.Task));

                    await Task.WhenAny(tasks).ConfigureAwait(false);

                    List<NetworkDlpClient> completedClients = clients.Where(c => c.Task.IsCompleted).ToList();
                    foreach (NetworkDlpClient completedClient in completedClients)
                    {
                        clients.Remove(completedClient);
                        await completedClient.DisposeAsync().ConfigureAwait(false);
                    }

                    if (!acceptTask.IsCompletedSuccessfully) continue;
                    TcpClient client = await acceptTask.ConfigureAwait(false);
                    NetworkDlpClient networkDlpClient = StartClient(client, syncHandler, innerCts.Token);
                    clients.Add(networkDlpClient);

                    if (!singleSync) continue;
                    await networkDlpClient.Task;
                    break;
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                tcpListener.Stop();
                
                innerCts.Cancel();
                try
                {
                    await Task.WhenAll(clients.Select(c => c.Task)).ConfigureAwait(false);
                }
                catch { /* Swallow */ }

                foreach (NetworkDlpClient client in clients)
                {
                    await client.DisposeAsync().ConfigureAwait(false);
                }
                
                innerCts.Dispose();
                
                Logger.ServerStopped(this, exception);
            }
        }

        public async Task HandleDeviceAsync(ISyncHandler syncHandler, TcpClient client, CancellationToken cancellationToken)
        {
            string connectionName = NetworkDlpConnection.GetConnectionName(client);
            Logger.ConnectionOpened(this, connectionName);

            Exception? exception = null;
            try
            {
                using TcpClient disposingClient = client;

                using CancellationTokenSource
                    ioCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                NetworkPipe devicePipe = new(client);

                using NetSyncInterface netSyncInterface = new(devicePipe, logger: _netSyncInterfaceLogger);
                Task netSyncIoTask = netSyncInterface.RunIOAsync(ioCts.Token);
                await using ConfiguredAsyncDisposable netSyncIoDispose =
                    AsyncDisposableCallback.EnsureCompletion(netSyncIoTask, ioCts).ConfigureAwait(false);

                NetSyncConnection netSyncConnection = new(netSyncInterface, logger: _netSyncConnectionLogger);
                await netSyncConnection.DoHandshakeAsync(cancellationToken).ConfigureAwait(false);

                IPEndPoint remoteEndPoint = (client.Client.RemoteEndPoint as IPEndPoint)!;
                DlpConnection dlpConnection =
                    new NetworkDlpConnection(remoteEndPoint, netSyncConnection, logger: _dlpConnectionLogger);
                await SyncAsync(dlpConnection, syncHandler, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                Logger.ConnectionClosed(this, connectionName, exception);
            }
        }

        private NetworkDlpClient StartClient(TcpClient client, ISyncHandler syncHandler,
            CancellationToken cancellationToken)
        {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            return new NetworkDlpClient
            {
                TcpClient = client,
                Task = Task.Run(() => HandleDeviceAsync(syncHandler, client, cts.Token), cts.Token),
                CancellationTokenSource = cts
            };
        }

        private class NetworkDlpClient : IAsyncDisposable
        {
            public required TcpClient TcpClient { get; init; }
            public required Task Task { get; init; }
            public required CancellationTokenSource CancellationTokenSource { get; init; }

            public async ValueTask DisposeAsync()
            {
                CancellationTokenSource.Cancel();

                try
                {
                    await Task;
                }
                catch
                {
                    // Swallow
                }
                
                TcpClient.Dispose();
                CancellationTokenSource.Dispose();
            }
        }

        private class NetworkDlpConnection : DlpConnection
        {
            public IPEndPoint RemoteEndPoint { get; }

            public NetworkDlpConnection(IPEndPoint remoteEndPoint, NetSyncConnection netSyncConnection, ArrayPool<byte>? arrayPool = null, ILogger? logger = null)
                : base(netSyncConnection, arrayPool, logger)
            {
                RemoteEndPoint = remoteEndPoint;
            }
            
            public NetworkDlpConnection(TcpClient client, NetSyncConnection netSyncConnection, ArrayPool<byte>? arrayPool = null, ILogger? logger = null)
                : this((IPEndPoint)client.Client.RemoteEndPoint!, netSyncConnection, arrayPool, logger)
            {
            }

            public static string GetConnectionName(IPEndPoint endPoint) => $"network@{endPoint}";
            public static string GetConnectionName(TcpClient client) => GetConnectionName((IPEndPoint)client.Client.RemoteEndPoint!);
            public override string ToString() => GetConnectionName(RemoteEndPoint);
        }
    }
}
