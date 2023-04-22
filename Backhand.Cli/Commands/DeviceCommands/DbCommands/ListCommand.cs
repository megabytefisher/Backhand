using Backhand.Cli.Internal.Commands;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseMetadata = Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListResponse.DatabaseMetadata;
using ReadDbListMode = Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListRequest.ReadDbListMode;

namespace Backhand.Cli.Commands.DeviceCommands.DbCommands
{
    public class ListCommand : BaseSyncCommand
    {
        private static readonly Option<IEnumerable<ReadDbListMode>> ReadModesOption =
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
                ListSyncHandler syncHandler = await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
                await RunDlpServerAsync(context, syncHandler).ConfigureAwait(false);
            });
        }

        public override async Task<ICommandSyncHandler> GetSyncHandlerAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            return await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
        }

        private Task<ListSyncHandler> GetSyncHandlerInternalAsync(InvocationContext context)
        {
            IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            ReadDbListMode readMode = context.ParseResult.GetValueForOption(ReadModesOption)!.Aggregate(ReadDbListMode.None, (acc, cur) => acc | cur);

            ListSyncHandler syncHandler = new()
            {
                ReadMode = readMode,
                Console = console
            };

            return Task.FromResult(syncHandler);
        }

        private class ListSyncHandler : CommandSyncHandler
        {
            public required ReadDbListMode ReadMode { get; init; }

            public override async Task OnSyncAsync(CommandSyncContext context, CancellationToken cancellationToken)
            {
                await context.Connection.OpenConduitAsync(cancellationToken).ConfigureAwait(false);

                List<DatabaseMetadata> dbResults = new();
                ushort startIndex = 0;
                while (true)
                {
                    try
                    {
                        ReadDbListResponse response = await context.Connection.ReadDbListAsync(new ReadDbListRequest
                        {
                            Mode = ReadMode,
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

        private static void PrintResults(IAnsiConsole console, DlpConnection connection, IEnumerable<DatabaseMetadata> metadataList)
        {
            Table table = new Table()
                .Title(Markup.Escape($"{connection} Database List"))
                .Expand()
                .AddColumn("Name")
                .AddColumn("MiscFlags")
                .AddColumn("Attributes")
                .AddColumn("Type")
                .AddColumn("Creator")
                .AddColumn("Version", c => c.RightAligned())
                .AddColumn("ModificationNumber", c => c.RightAligned())
                .AddColumn("CreationDate", c => c.RightAligned())
                .AddColumn("ModificationDate", c => c.RightAligned())
                .AddColumn("LastBackupDate", c => c.RightAligned());

            foreach (DatabaseMetadata metadata in metadataList)
            {
                table.AddRow(
                    Markup.Escape(metadata.Name),
                    Markup.Escape(metadata.MiscFlags.ToString()),
                    Markup.Escape(metadata.Attributes.ToString()),
                    Markup.Escape(metadata.Type),
                    Markup.Escape(metadata.Creator),
                    Markup.Escape(metadata.Version.ToString()),
                    Markup.Escape(metadata.ModificationNumber.ToString()),
                    Markup.Escape(metadata.CreationDate.ToString("s")),
                    Markup.Escape(metadata.ModificationDate.ToString("s")),
                    Markup.Escape(metadata.LastBackupDate.ToString("s"))
                );
            }

            console.Write(table);
        }
    }
}
