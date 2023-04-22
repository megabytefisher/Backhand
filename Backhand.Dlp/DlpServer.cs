using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public abstract class DlpServer : IDlpServer
    {
        public TimeSpan EndSyncTimeout { get; set; } = TimeSpan.FromMilliseconds(100);

        protected ILoggerFactory LoggerFactory { get; }
        protected ILogger Logger { get; }

        protected DlpServer(ILoggerFactory? loggerFactory = null)
        {
            LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            Logger = LoggerFactory.CreateLogger(DlpServerLogging.DlpServerCategory);
        }

        public abstract Task RunAsync(ISyncHandler syncHandler, bool singleSync, CancellationToken cancellationToken = default);

        protected async Task SyncAsync(DlpConnection connection, ISyncHandler syncHandler, CancellationToken cancellationToken = default)
        {
            Logger.StartingSync(this, connection);

            Exception? syncException = null;
            try
            {
                await syncHandler.OnSyncAsync(connection, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                syncException = ex;
            }

            try
            {
                using CancellationTokenSource endSyncCancellation = new(EndSyncTimeout);
                await connection.EndSyncAsync(new EndSyncRequest
                {
                    Status = syncException == null ?
                        EndSyncRequest.EndOfSyncStatus.Okay :
                        EndSyncRequest.EndOfSyncStatus.UnknownError
                }, endSyncCancellation.Token).ConfigureAwait(false);
            }
            catch
            {
                // Swallow.
            }
            
            Logger.SyncEnded(this, connection, syncException);
            
            if (syncException != null)
            {
                throw syncException;
            }
        }
    }
}
