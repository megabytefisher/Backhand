using System;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp
{
    public interface ISyncHandler
    {
        Task OnSyncAsync(DlpConnection connection, CancellationToken cancellationToken);
    }

    public abstract class SyncHandler<TContext> : ISyncHandler
    {
        public abstract Task<TContext> InitializeAsync(DlpConnection connection, CancellationToken cancellationToken);
        public virtual Task OnSyncStartedAsync(TContext context, CancellationToken cancellationToken) { return Task.CompletedTask; }
        public abstract Task OnSyncAsync(TContext context, CancellationToken cancellationToken);
        public virtual Task OnSyncEndedAsync(TContext context, Exception? exception, CancellationToken cancellationToken) { return Task.CompletedTask; }

        public async Task OnSyncAsync(DlpConnection connection, CancellationToken cancellationToken)
        {
            TContext context = await InitializeAsync(connection, cancellationToken);
            await OnSyncStartedAsync(context, cancellationToken);
            
            try
            {
                await OnSyncAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                await OnSyncEndedAsync(context, ex, cancellationToken);
                throw;
            }
            
            await OnSyncEndedAsync(context, null, cancellationToken);
        }
    }
}