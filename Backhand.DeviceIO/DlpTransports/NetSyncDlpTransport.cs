using System;
using System.Threading;
using Backhand.DeviceIO.NetSync;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpTransports
{
    public sealed class NetSyncDlpTransport : IDlpTransport
    {
        private readonly NetSyncConnection _netSync;

        public NetSyncDlpTransport(NetSyncConnection netSync)
        {
            _netSync = netSync;
        }

        public async Task ExecuteTransactionAsync(DlpPayload requestPayload, Action<DlpPayload> handleResponseAction,
            CancellationToken cancellationToken)
        {
            await _netSync.ExecuteTransactionAsync(
                new NetSyncPayload(requestPayload.Buffer),
                (responsePayload) =>
                {
                    handleResponseAction(new DlpPayload(responsePayload.Buffer));
                },
                cancellationToken).ConfigureAwait(false);
        }
    }
}
