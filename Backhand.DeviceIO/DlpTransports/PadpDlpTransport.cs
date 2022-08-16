using System;
using System.Threading;
using Backhand.DeviceIO.Padp;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpTransports
{
    public sealed class PadpDlpTransport : IDlpTransport
    {
        private readonly PadpConnection _padp;

        public PadpDlpTransport(PadpConnection padp)
        {
            _padp = padp;
        }

        public async Task ExecuteTransactionAsync(DlpPayload requestPayload, Action<DlpPayload> handleResponseAction,
            CancellationToken cancellationToken)
        {
            await _padp.ExecuteTransactionAsync(
                new PadpPayload(requestPayload.Buffer),
                (responsePayload) =>
                {
                    handleResponseAction(new DlpPayload(responsePayload.Buffer));
                },
                cancellationToken).ConfigureAwait(false);
        }
    }
}
