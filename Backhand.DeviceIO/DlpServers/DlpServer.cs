using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public abstract class DlpServer : IDlpServer
    {
        public event EventHandler<DlpSyncStartingEventArgs>? SyncStarting;
        public event EventHandler<DlpSyncEndedEventArgs>? SyncEnded;

        protected ILoggerFactory _loggerFactory;
        protected ILogger _logger;

        private Func<DlpContext, CancellationToken, Task> _syncFunc;

        public DlpServer(Func<DlpContext, CancellationToken, Task> syncFunc, ILoggerFactory? loggerFactory = null)
        {
            loggerFactory ??= NullLoggerFactory.Instance;

            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(GetType());

            _syncFunc = syncFunc;
        }

        public abstract Task Run(CancellationToken cancellationToken = default);

        protected async Task DoSync(DlpContext dlpContext, CancellationToken cancellationToken)
        {
            SyncStarting?.Invoke(this, new DlpSyncStartingEventArgs(dlpContext));

            Exception? syncException = null;
            try
            {
                await _syncFunc(dlpContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                syncException = ex;
            }

            try
            {
                await dlpContext.Connection.EndOfSyncAsync(new EndOfSyncRequest()
                {
                    Status = syncException == null ?
                        EndOfSyncRequest.EndOfSyncStatus.Okay :
                        EndOfSyncRequest.EndOfSyncStatus.UnknownError
                }, cancellationToken);

                // Give device time to read our message before tearing down the connection..
                await Task.Delay(100, cancellationToken);
            }
            catch
            {
            }

            SyncEnded?.Invoke(this, new DlpSyncEndedEventArgs(dlpContext, syncException));
        }
    }
}
