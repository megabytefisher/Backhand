using Backhand.Cli.Internal;
using Backhand.Dlp;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
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

        private readonly Option<IEnumerable<string>> ColumnsOption =
            new Option<IEnumerable<string>>(new[] { "--columns", "-c" }, () => MetadataColumns.Select(c => c.Header).ToArray())
            {
                AllowMultipleArgumentsPerToken = true
            }.FromAmong(MetadataColumns.Select(c => c.Header).ToArray());

        public ListCommand()
            : base("list", "Lists databases on a device")
        {
            this.Add(ReadModesOption);
            this.Add(ColumnsOption);

            this.SetHandler(async (context) =>
            {
                ReadDbListMode readMode = context.ParseResult.GetValueForOption(ReadModesOption)!.Aggregate(ReadDbListMode.None, (acc, cur) => acc | cur);
                IEnumerable<string> columns = context.ParseResult.GetValueForOption(ColumnsOption)!;

                IConsole console = context.Console;

                ListSyncHandler syncHandler = new()
                {
                    ReadMode = readMode,
                    Columns = columns,
                    Console = console
                };

                await RunDlpServerAsync<ListSyncContext>(context, syncHandler).ConfigureAwait(false);
            });
        }

        private class ListSyncContext
        {
            public required DlpConnection Connection { get; init; }
            public required ReadDbListMode ReadMode { get; init; }
            public required IEnumerable<string> Columns { get; init; }
            public required IConsole Console { get; init; }
        }

        private class ListSyncHandler : ISyncHandler<ListSyncContext>
        {
            public required ReadDbListMode ReadMode { get; init; }
            public required IEnumerable<string> Columns { get; init; }
            public required IConsole Console { get; init; }

            public Task<ListSyncContext> InitializeAsync(DlpConnection connection, CancellationToken cancellationToken)
            {
                return Task.FromResult(new ListSyncContext
                {
                    Connection = connection,
                    ReadMode = ReadMode,
                    Columns = Columns,
                    Console = Console
                });
            }

            public async Task OnSyncAsync(ListSyncContext context, CancellationToken cancellationToken)
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

                PrintResults(context.Console, context.Columns, dbResults);
            }
        }

        private static void PrintResults(IConsole console, IEnumerable<string> columnNames, ICollection<DatabaseMetadata> metadataList)
        {
            console.WriteTable(columnNames.Select(n => MetadataColumns.Single(c => c.Header == n)).ToList(), metadataList);
        }
        
        private static readonly IReadOnlyCollection<ConsoleTableColumn<DatabaseMetadata>> MetadataColumns = new[]
        {
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "Name",
                GetText = (metadata) => metadata.Name
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "MiscFlags",
                GetText = (metadata) => metadata.MiscFlags.ToString()
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "Attributes",
                GetText = (metadata) => metadata.Attributes.ToString()
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "Type",
                GetText = (metadata) => metadata.Type
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "Creator",
                GetText = (metadata) => metadata.Creator
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "Version",
                GetText = (metadata) => metadata.Version.ToString(),
                IsRightAligned = true
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "ModificationNumber",
                GetText = (metadata) => metadata.ModificationNumber.ToString(),
                IsRightAligned = true
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "CreationDate",
                GetText = (metadata) => metadata.CreationDate.ToString("s"),
                IsRightAligned = true
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "ModificationDate",
                GetText = (metadata) => metadata.ModificationDate.ToString("s"),
                IsRightAligned = true
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "LastBackupDate",
                GetText = (metadata) => metadata.LastBackupDate.ToString("s"),
                IsRightAligned = true
            }
        };
    }
}
