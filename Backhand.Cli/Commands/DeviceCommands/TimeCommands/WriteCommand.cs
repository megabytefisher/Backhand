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
    public class WriteCommand : BaseSyncCommand
    {
        private static readonly Option<DateTime?> TimeOption =
            new(new[] { "--time", "-t" }, "The date to set on the device");

        public WriteCommand()
            : base("write", "Writes time and date to a device")
        {
            this.AddOption(TimeOption);

            this.SetHandler(async (context) =>
            {
                DateTime? time = context.ParseResult.GetValueForOption(TimeOption);

                IConsole console = context.Console;

                WriteSyncHandler syncHandler = new()
                {
                    Time = time,
                    Console = console
                };

                await RunDlpServerAsync<WriteSyncContext>(context, syncHandler).ConfigureAwait(false);
            });
        }

        private class WriteSyncContext
        {
            public required DlpConnection Connection { get; init; }
            public required DateTime? Time { get; init; }
            public required IConsole Console { get; init; }
        }

        private class WriteSyncHandler : ISyncHandler<WriteSyncContext>
        {
            public required DateTime? Time { get; init; }
            public required IConsole Console { get; init; }

            public Task<WriteSyncContext> InitializeAsync(DlpConnection connection, CancellationToken cancellationToken)
            {
                return Task.FromResult(new WriteSyncContext
                {
                    Connection = connection,
                    Time = Time,
                    Console = Console
                });
            }

            public async Task OnSyncAsync(WriteSyncContext context, CancellationToken cancellationToken)
            {
                await context.Connection.WriteSysDateTimeAsync(new() {
                    DateTime = context.Time ?? DateTime.Now
                }, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}