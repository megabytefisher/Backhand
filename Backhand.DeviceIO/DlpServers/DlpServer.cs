using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public abstract class DlpServer
    {
        protected ILoggerFactory _loggerFactory;
        protected ILogger _logger;

        private Func<DlpConnection, CancellationToken, Task> _syncFunc;

        public DlpServer(Func<DlpConnection, CancellationToken, Task> syncFunc, ILoggerFactory? loggerFactory = null)
        {
            loggerFactory ??= NullLoggerFactory.Instance;

            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(GetType());

            _syncFunc = syncFunc;
        }

        public abstract Task Run(CancellationToken cancellationToken = default);

        protected async Task DoSync(DlpConnection dlpConnection, CancellationToken cancellationToken)
        {
            try
            {
                await dlpConnection.ReadUserInfoAsync(cancellationToken).ConfigureAwait(false);
                await dlpConnection.ReadSysInfoAsync(new ReadSysInfoRequest
                {
                    HostDlpVersionMajor = 1,
                    HostDlpVersionMinor = 4,
                }, cancellationToken).ConfigureAwait(false);

                await _syncFunc(dlpConnection, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DoSync failed");
                Console.WriteLine(ex);
            }

            await dlpConnection.EndOfSyncAsync(new EndOfSyncRequest()
            {
                Status = EndOfSyncRequest.EndOfSyncStatus.Okay
            }, cancellationToken);

            // Give device time to read our message before tearing down the connection..
            await Task.Delay(50, cancellationToken);
        }
    }
}
