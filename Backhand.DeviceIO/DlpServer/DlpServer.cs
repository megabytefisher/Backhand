using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.Dlp.Arguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServer
{
    public abstract class DlpServer
    {
        public abstract Task Run(CancellationToken cancellationToken = default);

        protected async Task DoSync(DlpConnection dlpConnection, CancellationToken cancellationToken = default)
        {
            await SendEndOfSync(dlpConnection, cancellationToken).ConfigureAwait(false);
        }

        private async Task SendEndOfSync(DlpConnection dlpConnection, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection endOfSyncRequestArgs = new DlpArgumentCollection();
            endOfSyncRequestArgs.SetValue(DlpCommandDefinitions.EndOfSyncArgs.EndOfSyncRequest, new EndOfSyncRequest()
            {
                Status = EndOfSyncRequest.EndOfSyncStatus.Okay
            });
            await dlpConnection.Execute(DlpCommandDefinitions.EndOfSync, endOfSyncRequestArgs).ConfigureAwait(false);
        }
    }
}
