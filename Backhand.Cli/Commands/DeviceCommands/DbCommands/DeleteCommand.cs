using Backhand.Cli.Internal.Commands;
using Backhand.PalmDb;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backhand.PalmDb.Dlp;

namespace Backhand.Cli.Commands.DeviceCommands.DbCommands
{
    public class DeleteCommand : BaseSyncCommand
    {
        private static readonly Argument<string[]> NamesArgument =
            new("names", "Name(s) of databases to delete")
            {
                Arity = ArgumentArity.OneOrMore
            };

        public DeleteCommand() : base("delete", "Deletes one or more databases from a connected device")
        {
            Add(NamesArgument);

            this.SetHandler(async (context) =>
            {
                PullSyncHandler syncHandler = await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
                await RunDlpServerAsync(context, syncHandler).ConfigureAwait(false);
            });
        }

        public override async Task<ICommandSyncHandler> GetSyncHandlerAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            return await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
        }

        private Task<PullSyncHandler> GetSyncHandlerInternalAsync(InvocationContext context)
        {
            IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            string[] names = context.ParseResult.GetValueForArgument(NamesArgument);

            PullSyncHandler syncHandler = new()
            {
                Console = console,
                Names = names
            };

            return Task.FromResult(syncHandler);
        }

        private class PullSyncHandler : CommandSyncHandler
        {
            public required string[] Names { get; init; }

            public override async Task OnSyncAsync(CommandSyncContext context, CancellationToken cancellationToken)
            {
                await context.Client.OpenConduitAsync(cancellationToken).ConfigureAwait(false);
                
                DlpDatabaseRepository deviceDbRepository = new(context.Client);
                ICollection<PalmDbHeader> deviceDbHeaders =
                    await deviceDbRepository.GetHeadersAsync(cancellationToken);

                foreach (string name in Names)
                {
                    context.Console.MarkupLineInterpolated($"[grey]Deleting database {name}[/]");
                    
                    PalmDbHeader? header = deviceDbHeaders.FirstOrDefault(h => h.Name == name);
                    if (header == null)
                    {
                        context.Console.MarkupLineInterpolated($"[red]Database {name} not found.[/]");
                        continue;
                    }
                    
                    await deviceDbRepository.DeleteDatabaseAsync(header, cancellationToken).ConfigureAwait(false);
                    context.Console.MarkupLineInterpolated($"[green]Deleted database {name}.[/]");
                }
            }
        }
    }
}