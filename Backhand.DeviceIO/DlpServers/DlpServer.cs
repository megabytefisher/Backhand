using Backhand.DeviceIO.DlpCommands.v1_0;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public abstract class DlpServer : IDlpServer
    {
        public event EventHandler<DlpSyncStartingEventArgs>? SyncStarting;
        public event EventHandler<DlpSyncEndedEventArgs>? SyncEnded;

        protected ILoggerFactory LoggerFactory { get; }
        protected ILogger Logger { get; }

        private readonly Func<DlpClientContext, CancellationToken, Task> _syncFunc;

        protected DlpServer(Func<DlpClientContext, CancellationToken, Task> syncFunc, ILoggerFactory? loggerFactory = null)
        {
            loggerFactory ??= NullLoggerFactory.Instance;

            LoggerFactory = loggerFactory;
            Logger = loggerFactory.CreateLogger(GetType());

            _syncFunc = syncFunc;
        }

        public abstract Task RunAsync(CancellationToken cancellationToken = default);

        protected async Task DoSyncAsync(DlpClientContext dlpClientContext, CancellationToken cancellationToken)
        {
            SyncStarting?.Invoke(this, new DlpSyncStartingEventArgs(dlpClientContext));

            Exception? syncException = null;
            try
            {
                await _syncFunc(dlpClientContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                syncException = ex;
            }

            try
            {
                await dlpClientContext.Connection.EndOfSyncAsync(new EndOfSyncRequest
                {
                    Status = syncException == null ?
                        EndOfSyncRequest.EndOfSyncStatus.Okay :
                        EndOfSyncRequest.EndOfSyncStatus.UnknownError
                }, cancellationToken).ConfigureAwait(false);

                // Give device time to read our message before tearing down the connection..
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }

            SyncEnded?.Invoke(this, new DlpSyncEndedEventArgs(dlpClientContext, syncException));
        }
    }
}
