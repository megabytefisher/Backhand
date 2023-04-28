using System;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands.v1_0;

namespace Backhand.Dlp
{
    public interface ISyncHandler
    {
        Task OnSyncAsync(DlpClient connection, CancellationToken cancellationToken);
    }

    public abstract class SyncHandler<TContext> : ISyncHandler
    {
        public abstract Task<TContext> InitializeAsync(DlpClient client, CancellationToken cancellationToken);
        public virtual Task OnSyncStartedAsync(TContext context, CancellationToken cancellationToken) { return Task.CompletedTask; }
        public abstract Task OnSyncAsync(TContext context, CancellationToken cancellationToken);
        public virtual Task OnSyncEndedAsync(TContext context, Exception? exception, CancellationToken cancellationToken) { return Task.CompletedTask; }

        public async Task OnSyncAsync(DlpClient client, CancellationToken cancellationToken)
        {
            TContext context = await InitializeAsync(client, cancellationToken);
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