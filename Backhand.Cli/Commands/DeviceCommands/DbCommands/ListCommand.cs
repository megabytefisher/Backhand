using Backhand.Cli.Internal;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using Spectre.Console;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseMetadata = Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListResponse.DatabaseMetadata;
using ReadDbListMode = Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListRequest.ReadDbListMode;

namespace Backhand.Cli.Commands.DeviceCommands.DbCommands
{
    public class ListCommand : BaseSyncCommand
    {
        private readonly Option<IEnumerable<ReadDbListMode>> ReadModesOption =
            new(new[] { "--read-modes", "-m" }, () => new[] { ReadDbListMode.ListMultiple | ReadDbListMode.ListRam })
            {
                AllowMultipleArgumentsPerToken = true
            };

        public ListCommand()
            : base("list", "Lists databases on a device")
        {
            Add(ReadModesOption);

            this.SetHandler(async (context) =>
            {
                ReadDbListMode readMode = context.ParseResult.GetValueForOption(ReadModesOption)!.Aggregate(ReadDbListMode.None, (acc, cur) => acc | cur);

                IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();

                ListSyncHandler syncHandler = new()
                {
                    ReadMode = readMode,
                    Console = console
                };

                await RunDlpServerAsync(context, syncHandler).ConfigureAwait(false);
            });
        }

        private class ListSyncContext : SyncContext
        {
            public required ReadDbListMode ReadMode { get; init; }
        }

        private class ListSyncHandler : SyncHandler<ListSyncContext>
        {
            public required ReadDbListMode ReadMode { get; init; }

            protected override Task<ListSyncContext> GetContextAsync(DlpConnection connection, CancellationToken cancellationToken)
            {
                return Task.FromResult(new ListSyncContext
                {
                    Connection = connection,
                    Console = Console,
                    ReadMode = ReadMode,
                });
            }

            public override async Task OnSyncAsync(ListSyncContext context, CancellationToken cancellationToken)
            {
                await context.Connection.OpenConduitAsync(cancellationToken).ConfigureAwait(false);

                List<DatabaseMetadata> dbResults = new List<DatabaseMetadata>();
                ushort startIndex = 0;
                while (true)
                {
                    try
                    {
                        ReadDbListResponse response = await context.Connection.ReadDbListAsync(new ReadDbListRequest
                        {
                            Mode = context.ReadMode,
                            StartIndex = startIndex
                        }, cancellationToken).ConfigureAwait(false);

                        dbResults.AddRange(response.Results);
                        startIndex = (ushort)(response.LastIndex + 1);
                    }
                    catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
                    {
                        break;
                    }
                }

                PrintResults(context.Console, context.Connection, dbResults);
            }
        }

        private static void PrintResults(IAnsiConsole console, DlpConnection connection, ICollection<DatabaseMetadata> metadataList)
        {
            Table table = new Table()
            {
                Title = new TableTitle($"{connection.ToString()} Database Listing"),
                Expand = true
            };

            table.AddColumn(new TableColumn(new Markup("[bold]Name[/]")));
            table.AddColumn("MiscFlags");
            table.AddColumn("Attributes");
            table.AddColumn("Type");
            table.AddColumn("Creator");
            table.AddColumn("Version", c => c.RightAligned());
            table.AddColumn("ModificationNumber", c => c.RightAligned());
            table.AddColumn("CreationDate", c => c.RightAligned());
            table.AddColumn("ModificationDate", c => c.RightAligned());
            table.AddColumn("LastBackupDate", c => c.RightAligned());

            foreach (DatabaseMetadata metadata in metadataList)
            {
                table.AddRow(
                    Markup.FromInterpolated($"[bold]{metadata.Name}[/]"),
                    Markup.FromInterpolated($"{metadata.MiscFlags}"),
                    Markup.FromInterpolated($"{metadata.Attributes}"),
                    Markup.FromInterpolated($"{metadata.Type}"),
                    Markup.FromInterpolated($"{metadata.Creator}"),
                    Markup.FromInterpolated($"{metadata.Version}"),
                    Markup.FromInterpolated($"{metadata.ModificationNumber}"),
                    Markup.FromInterpolated($"{metadata.CreationDate:s}"),
                    Markup.FromInterpolated($"{metadata.ModificationDate:s}"),
                    Markup.FromInterpolated($"{metadata.LastBackupDate:s}")
                );
            }

            console.Write(table);
        }
    }
}
