using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands.v1_0;

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
            
            // Get a client for the device's DLP version.
            DlpClient client = await GetDlpClientAsync(connection, cancellationToken).ConfigureAwait(false);

            Exception? syncException = null;
            try
            {
                await syncHandler.OnSyncAsync(client, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                syncException = ex;
            }

            try
            {
                using CancellationTokenSource endSyncCancellation = new(EndSyncTimeout);
                await client.EndSyncAsync(new EndSyncRequest
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

        private async Task<DlpClient> GetDlpClientAsync(DlpConnection connection, CancellationToken cancellationToken = default)
        {
            // Read SysInfo to get the device's DLP version.
            
            // TODO : Account for devices < v1.2
            DlpClient clientV1_2 = new(connection);

            return clientV1_2;
        }
    }
}
