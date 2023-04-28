using Backhand.Cli.Internal.Commands;
using Backhand.PalmDb;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Backhand.PalmDb.Dlp;
using Backhand.PalmDb.FileIO;

namespace Backhand.Cli.Commands.DeviceCommands.DbCommands
{
    public class PushCommand : BaseSyncCommand
    {
        private static readonly Argument<FileInfo[]> PathArgument =
            new("path", "Path(s) to database files to push")
            {
                Arity = ArgumentArity.OneOrMore
            };

        public PushCommand() : base("push", "Writes one or more databases to a connected device")
        {
            Add(PathArgument);

            this.SetHandler(async (context) =>
            {
                PushSyncHandler syncHandler = await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
                await RunDlpServerAsync(context, syncHandler).ConfigureAwait(false);
            });
        }

        public override async Task<ICommandSyncHandler> GetSyncHandlerAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            return await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
        }

        private Task<PushSyncHandler> GetSyncHandlerInternalAsync(InvocationContext context)
        {
            IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            FileInfo[] paths = context.ParseResult.GetValueForArgument(PathArgument);

            PushSyncHandler syncHandler = new()
            {
                Console = console,
                Paths = paths
            };

            return Task.FromResult(syncHandler);
        }

        private class PushSyncHandler : CommandSyncHandler
        {
            public required FileInfo[] Paths { get; init; }

            public override async Task OnSyncAsync(CommandSyncContext context, CancellationToken cancellationToken)
            {
                await context.Client.OpenConduitAsync(cancellationToken).ConfigureAwait(false);
                
                DirectoryInfo currentDirectory = new(Directory.GetCurrentDirectory());

                DirectoryDbRepository directoryDbRepository = new(currentDirectory);
                DlpDatabaseRepository deviceDbRepository = new(context.Client);

                foreach (FileInfo path in Paths)
                {
                    context.Console.MarkupLineInterpolated($"[grey]Pushing database: {path.FullName}[/]");
                    
                    PalmDbHeader fileDbHeader = await PalmDbFile.ReadHeaderAsync(path, cancellationToken).ConfigureAwait(false);
                    IPalmDb fileDb = await directoryDbRepository.OpenDatabaseAsync(fileDbHeader, cancellationToken).ConfigureAwait(false);
                    IPalmDb newDb = await fileDb.CopyToAsync(deviceDbRepository, cancellationToken).ConfigureAwait(false);
                    await deviceDbRepository.CloseDatabaseAsync(newDb, cancellationToken).ConfigureAwait(false);
                    await directoryDbRepository.CloseDatabaseAsync(fileDb, cancellationToken).ConfigureAwait(false);

                    context.Console.MarkupLineInterpolated($"[green]Pushed database: {path.FullName}[/]");
                }
            }
        }
    }
}