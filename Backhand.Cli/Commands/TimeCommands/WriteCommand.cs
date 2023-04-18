using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Protocols.Dlp;

namespace Backhand.Cli.Commands.TimeCommands
{
    public class WriteCommand : SyncFuncCommand
    {
        private class SyncContext
        {
            public DateTime? Time { get; init; }
            public required IConsole Console { get; init; }
        }

        private static readonly Option<DateTime?> TimeOption = new(new[] { "--time", "-t" }, "The date to set on the device");

        public WriteCommand() : base("write", "Writes time and date to a device")
        {
            this.AddOption(TimeOption);

            this.SetHandler(async (context) =>
            {
                DateTime? time = context.ParseResult.GetValueForOption(TimeOption);

                IConsole console = context.Console;

                Func<DlpConnection, SyncContext> contextFactory = _ => new SyncContext
                {
                    Time = time,
                    Console = console
                };

                await RunDlpServerAsync(context, SyncAsync, contextFactory).ConfigureAwait(false);
            });
        }
        
        private async Task SyncAsync(DlpConnection connection, SyncContext context, CancellationToken cancellationToken)
        {
            await connection.WriteSysDateTimeAsync(new() {
                DateTime = context.Time ?? DateTime.Now
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}