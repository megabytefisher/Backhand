using Backhand.Cli.Internal.Commands;
using Backhand.PalmDb;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backhand.PalmDb.Dlp;
using Backhand.PalmDb.FileIO;

namespace Backhand.Cli.Commands.DeviceCommands.DbCommands
{
    public class PullCommand : BaseSyncCommand
    {
        private static readonly Argument<string[]> NamesArgument =
            new("names", "Name(s) of databases to pull")
            {
                Arity = ArgumentArity.OneOrMore
            };

        public PullCommand() : base("pull", "Reads one or more databases from a connected device")
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
                
                DirectoryInfo currentDirectory = new(Directory.GetCurrentDirectory());
                
                DirectoryDbRepository directoryDbRepository = new(currentDirectory);
                DlpDatabaseRepository deviceDbRepository = new(context.Client);
                
                ICollection<PalmDbHeader> deviceDbHeaders =
                    await deviceDbRepository.GetHeadersAsync(cancellationToken);
                
                foreach (string name in Names)
                {
                    // Try to get header
                    PalmDbHeader? deviceDbHeader = deviceDbHeaders
                        .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                    if (deviceDbHeader == null)
                    {
                        context.Console.MarkupLineInterpolated($"[red]Database not found: {name}[/]");
                    }
                    else
                    {
                        context.Console.MarkupLineInterpolated($"[grey]Pulling database: {name}[/]");

                        IPalmDb deviceDb = await deviceDbRepository.OpenDatabaseAsync(deviceDbHeader, cancellationToken).ConfigureAwait(false);
                        IPalmDb newDb = await deviceDb.CopyToAsync(deviceDbRepository, cancellationToken).ConfigureAwait(false);
                        await deviceDbRepository.CloseDatabaseAsync(deviceDb, cancellationToken).ConfigureAwait(false);
                        await directoryDbRepository.CloseDatabaseAsync(newDb, cancellationToken).ConfigureAwait(false);
                        
                        context.Console.MarkupLineInterpolated($"[green]Pulled database: {name}[/]");
                    }
                }
            }
        }
    }
}