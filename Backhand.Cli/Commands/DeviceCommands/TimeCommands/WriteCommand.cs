using Backhand.Cli.Internal.Commands;
using Backhand.Dlp.Commands.v1_0;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands.DeviceCommands.TimeCommands
{
    public class WriteCommand : BaseSyncCommand
    {
        private static readonly Option<DateTime?> TimeOption =
            new(new[] { "--time", "-t" }, "The date to set on the device");

        public WriteCommand()
            : base("write", "Writes time and date to a device")
        {
            Add(TimeOption);

            this.SetHandler(async (context) =>
            {
                WriteSyncHandler syncHandler = await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
                await RunDlpServerAsync(context, syncHandler).ConfigureAwait(false);
            });
        }

        public override async Task<ICommandSyncHandler> GetSyncHandlerAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            return await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
        }

        private Task<WriteSyncHandler> GetSyncHandlerInternalAsync(InvocationContext context)
        {
            IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            DateTime? time = context.ParseResult.GetValueForOption(TimeOption);

            WriteSyncHandler syncHandler = new()
            {
                Console = console,
                Time = time
            };

            return Task.FromResult(syncHandler);
        }

        private class WriteSyncHandler : CommandSyncHandler
        {
            public required DateTime? Time { get; init; }

            public override async Task OnSyncAsync(CommandSyncContext context, CancellationToken cancellationToken)
            {
                DateTime writeTime = Time ?? DateTime.Now;
                
                await context.Connection.WriteSysDateTimeAsync(new()
                {
                    DateTime = writeTime
                }, cancellationToken).ConfigureAwait(false);
                
                context.Console.MarkupLineInterpolated($"[green]Wrote device date: {writeTime:s}[/]");
            }
        }
    }
}