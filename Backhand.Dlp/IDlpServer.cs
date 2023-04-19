using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public interface IDlpServer<TContext>
    {
        Task RunAsync(ISyncHandler<TContext> syncHandler, CancellationToken cancellationToken = default);
    }
}
