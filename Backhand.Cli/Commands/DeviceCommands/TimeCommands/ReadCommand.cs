using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Cli.Internal;
using Backhand.Dlp;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Protocols.Dlp;

namespace Backhand.Cli.Commands.DeviceCommands.TimeCommands
{
    public class ReadCommand : BaseSyncCommand
    {
        public ReadCommand()
            : base("read", "Reads time and date from a device")
        {
            this.SetHandler(async (context) =>
            {
                IConsole console = context.Console;

                ReadSyncHandler syncHandler = new()
                {
                    Console = console
                };

                await RunDlpServerAsync<ReadSyncContext>(context, syncHandler).ConfigureAwait(false);
            });
        }

        private class ReadSyncContext
        {
            public required DlpConnection Connection { get; init; }
            public required IConsole Console { get; init; }
        }

        private class ReadSyncHandler : ISyncHandler<ReadSyncContext>
        {
            public required IConsole Console { get; init; }

            public Task<ReadSyncContext> InitializeAsync(DlpConnection connection, CancellationToken cancellationToken)
            {
                return Task.FromResult(new ReadSyncContext
                {
                    Connection = connection,
                    Console = Console
                });
            }

            public async Task OnSyncAsync(ReadSyncContext context, CancellationToken cancellationToken)
            {
                var systemDate = await context.Connection.ReadSysDateTimeAsync(cancellationToken).ConfigureAwait(false);
                context.Console.WriteLine($"Got device date: {systemDate.DateTime}");
            }
        }
    }
}