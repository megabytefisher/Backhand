using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.DeviceIO
{
    public interface IDlpTransport
    {
        Task ExecuteTransactionAsync(ReadOnlySequence<byte> requestPayload, Action<ReadOnlySequence<byte>> responseCallback, CancellationToken cancellationToken = default);
    }
}
