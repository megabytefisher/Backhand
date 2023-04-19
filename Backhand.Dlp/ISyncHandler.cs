using System;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp
{
    public interface ISyncHandler<TContext>
    {
        Task<TContext> InitializeAsync(DlpConnection connection, CancellationToken cancellationToken);
        Task OnSyncStartedAsync(TContext context, CancellationToken cancellationToken) { return Task.CompletedTask; }
        Task OnSyncAsync(TContext context, CancellationToken cancellationToken);
        Task OnSyncEndedAsync(TContext context, Exception? exception, CancellationToken cancellationToken) { return Task.CompletedTask; }
    }
}