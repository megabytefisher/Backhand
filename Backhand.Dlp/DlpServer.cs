using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public abstract class DlpServer<TContext> : IDlpServer<TContext>
    {
        public event EventHandler<DlpSyncStartingEventArgs<TContext>>? SyncStarting;
        public event EventHandler<DlpSyncEndedEventArgs<TContext>>? SyncEnded;

        private DlpSyncFunc<TContext> _syncFunc;
        private Func<DlpConnection, TContext> _contextFactory;

        private static readonly TimeSpan EndSyncDelay = TimeSpan.FromMilliseconds(100);

        protected DlpServer(DlpSyncFunc<TContext> syncFunc, Func<DlpConnection, TContext>? contextFactory)
        {
            _syncFunc = syncFunc;
            _contextFactory = contextFactory ?? (connection => Activator.CreateInstance<TContext>());
        }

        public abstract Task RunAsync(bool singleSync = false, CancellationToken cancellationToken = default);

        protected async Task DoSyncAsync(DlpConnection connection, CancellationToken cancellationToken = default)
        {
            TContext context = _contextFactory(connection);

            SyncStarting?.Invoke(this, new DlpSyncStartingEventArgs<TContext>(connection, context));

            Exception? syncException = null;
            try
            {
                await _syncFunc(connection, context, cancellationToken).ConfigureAwait(false);
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

            SyncEnded?.Invoke(this, new DlpSyncEndedEventArgs<TContext>(connection, context, syncException));
            await Task.Delay(EndSyncDelay);
        }
    }
}
