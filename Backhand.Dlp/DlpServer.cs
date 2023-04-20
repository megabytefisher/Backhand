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
    public abstract class DlpServer<TContext> : IDlpServer<TContext>
    {
        protected ILoggerFactory LoggerFactory { get; }
        protected ILogger Logger { get; }

        private static readonly TimeSpan EndSyncDelay = TimeSpan.FromMilliseconds(100);

        public DlpServer(ILoggerFactory? loggerFactory = null)
        {
            LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            Logger = LoggerFactory.CreateLogger(GetType());
        }

        public abstract Task RunAsync(ISyncHandler<TContext> context, CancellationToken cancellationToken);

        protected async Task HandleConnection(DlpConnection connection, ISyncHandler<TContext> syncHandler, CancellationToken cancellationToken = default)
        {
            TContext context = await syncHandler.InitializeAsync(connection, cancellationToken).ConfigureAwait(false);

            await syncHandler.OnSyncStartedAsync(context, cancellationToken).ConfigureAwait(false);

            Exception? syncException = null;
            try
            {
                await syncHandler.OnSyncAsync(context, cancellationToken).ConfigureAwait(false);
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

            await syncHandler.OnSyncEndedAsync(context, syncException, cancellationToken).ConfigureAwait(false);

            await Task.Delay(EndSyncDelay, cancellationToken).ConfigureAwait(false);
        }
    }
}
