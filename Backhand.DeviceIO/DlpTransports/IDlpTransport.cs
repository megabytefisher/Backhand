using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpTransports
{
    public interface IDlpTransport
    {
        Task ExecuteTransactionAsync(DlpPayload requestPayload, Action<DlpPayload> handleResponseAction,
            CancellationToken cancellationToken);
    }
}