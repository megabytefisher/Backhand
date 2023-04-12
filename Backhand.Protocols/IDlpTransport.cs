using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Protocols
{
    public interface IDlpTransport
    {
        Task ExecuteTransactionAsync(ReadOnlySequence<byte> requestPayload, Action<ReadOnlySequence<byte>> responseCallback, CancellationToken cancellationToken = default);
    }
}
