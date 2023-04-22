using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public interface IDlpServer
    {
        Task RunAsync(ISyncHandler syncHandler, bool singleSync = false, CancellationToken cancellationToken = default);
    }
}
