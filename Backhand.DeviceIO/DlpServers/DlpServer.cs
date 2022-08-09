using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public abstract class DlpServer
    {
        private Func<DlpConnection, CancellationToken, Task> _syncFunc;

        public DlpServer(Func<DlpConnection, CancellationToken, Task> syncFunc)
        {
            _syncFunc = syncFunc;
        }

        public abstract Task Run(CancellationToken cancellationToken = default);

        protected async Task DoSync(DlpConnection dlpConnection, CancellationToken cancellationToken = default)
        {
            try
            {
                await _syncFunc(dlpConnection, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DoSync failed");
                Console.WriteLine(ex);
            }

            await dlpConnection.EndOfSync(new EndOfSyncRequest()
            {
                Status = EndOfSyncRequest.EndOfSyncStatus.Okay
            });

            // Give device time to read our message before tearing down the connection..
            await Task.Delay(50);
        }
    }
}
