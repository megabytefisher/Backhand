using Backhand.Cli.Internal.Commands;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands.v1_0.Arguments;

namespace Backhand.Cli.Commands.DeviceCommands.TimeCommands
{
    public class ReadCommand : BaseSyncCommand
    {
        public ReadCommand()
            : base("read", "Reads time and date from a device")
        {
            this.SetHandler(async (context) =>
            {
                ReadSyncHandler syncHandler = await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
                await RunDlpServerAsync(context, syncHandler).ConfigureAwait(false);
            });
        }

        public override async Task<ICommandSyncHandler> GetSyncHandlerAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            return await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
        }

        private Task<ReadSyncHandler> GetSyncHandlerInternalAsync(InvocationContext context)
        {
            IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            ReadSyncHandler syncHandler = new()
            {
                Console = console
            };

            return Task.FromResult(syncHandler);
        }

        private class ReadSyncHandler : CommandSyncHandler
        {
            public override async Task OnSyncAsync(CommandSyncContext context, CancellationToken cancellationToken)
            {
                ReadSysDateTimeResponse systemDateResponse = await context.Client.ReadSysDateTimeAsync(cancellationToken).ConfigureAwait(false);
                context.Console.MarkupLineInterpolated($"[green]Got device date: {systemDateResponse.DateTime:s}[/]");
            }
        }
    }
}