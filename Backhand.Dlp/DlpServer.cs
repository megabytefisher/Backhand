using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public abstract class DlpServer : IDlpServer
    {
        public event EventHandler<DlpSyncStartingEventArgs>? SyncStarting;
        public event EventHandler<DlpSyncEndedEventArgs>? SyncEnded;

        private DlpSyncFunc _syncFunc;

        private static readonly TimeSpan EndSyncDelay = TimeSpan.FromMilliseconds(100);

        protected DlpServer(DlpSyncFunc syncFunc)
        {
            _syncFunc = syncFunc;
        }

        public abstract Task RunAsync(CancellationToken cancellationToken = default);

        protected async Task DoSyncAsync(DlpConnection connection, CancellationToken cancellationToken = default)
        {
            SyncStarting?.Invoke(this, new DlpSyncStartingEventArgs(connection));

            Exception? syncException = null;
            try
            {
                await _syncFunc(connection, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                syncException = ex;
            }

            try
            {
                await connection.EndSyncAsync(new()
                {
                    Status = syncException == null ?
                        EndSyncRequest.EndOfSyncStatus.Okay :
                        EndSyncRequest.EndOfSyncStatus.UnknownError
                }, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Swallow.
            }

            SyncEnded?.Invoke(this, new DlpSyncEndedEventArgs(connection, syncException));
            await Task.Delay(EndSyncDelay);
        }
    }
}
