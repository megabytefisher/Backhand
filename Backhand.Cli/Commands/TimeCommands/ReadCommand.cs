using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Protocols.Dlp;

namespace Backhand.Cli.Commands.TimeCommands
{
    public class ReadCommand : SyncFuncCommand
    {
        private class SyncContext
        {
            public required IConsole Console { get; init; }
        }

        public ReadCommand() : base("read", "Reads time and date from a device")
        {
            this.SetHandler(async (context) =>
            {
                IConsole console = context.Console;

                Func<DlpConnection, SyncContext> contextFactory = _ => new()
                {
                    Console = console
                };

                await RunDlpServerAsync(context, SyncAsync, contextFactory).ConfigureAwait(false);
            });
        }
        
        private async Task SyncAsync(DlpConnection connection, SyncContext context, CancellationToken cancellationToken)
        {
            var systemDate = await connection.ReadSysDateTimeAsync(cancellationToken).ConfigureAwait(false);
            context.Console.WriteLine($"Got device date: {systemDate.DateTime}");
        }
    }
}