using Backhand.Cli.Internal;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseMetadata = Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListResponse.DatabaseMetadata;
using ReadDbListMode = Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListRequest.ReadDbListMode;

namespace Backhand.Cli.Commands.DbCommands
{
    public class ListCommand : SyncFuncCommand
    {
        private class SyncContext
        {
            public required ReadDbListMode ReadMode { get; init; }
            public required IEnumerable<string> Columns { get; init; }
            public required IConsole Console { get; init; }
        }

        private readonly Option<IEnumerable<ReadDbListMode>> ReadModesOption = new(new[] { "--read-modes", "-m" }, () => new[] { ReadDbListMode.ListMultiple | ReadDbListMode.ListRam })
        {
            AllowMultipleArgumentsPerToken = true
        };

        private readonly Option<IEnumerable<string>> ColumnsOption = new Option<IEnumerable<string>>(new[] { "--columns", "-c" }, () => new[] { "Name", "Attributes", "Type", "Creator" })
        {
            AllowMultipleArgumentsPerToken = true
        }.FromAmong(MetadataColumns.Select(c => c.Header).ToArray());

        public ListCommand() : base("list", "Lists databases on a device")
        {
            this.Add(ReadModesOption);
            this.Add(ColumnsOption);

            this.SetHandler(async (context) =>
            {
                ReadDbListMode readMode = context.ParseResult.GetValueForOption(ReadModesOption)!.Aggregate(ReadDbListMode.None, (acc, cur) => acc | cur);
                IEnumerable<string> columns = context.ParseResult.GetValueForOption(ColumnsOption)!;

                IConsole console = context.Console;

                Func<DlpConnection, SyncContext> contextFactory = _ => new SyncContext
                {
                    ReadMode = readMode,
                    Columns = columns,
                    Console = console
                };

                await RunDlpServerAsync<SyncContext>(context, SyncAsync, contextFactory).ConfigureAwait(false);
            });
        }

        private async Task SyncAsync(DlpConnection connection, SyncContext context, CancellationToken cancellationToken)
        {
            await connection.OpenConduitAsync(cancellationToken).ConfigureAwait(false);

            List<DatabaseMetadata> metadataList = new List<DatabaseMetadata>();
            ushort startIndex = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    ReadDbListResponse dbListResponse = await connection.ReadDbListAsync(new()
                    {
                        Mode = context.ReadMode,
                        CardId = 0,
                        StartIndex = startIndex
                    }, cancellationToken).ConfigureAwait(false);

                    metadataList.AddRange(dbListResponse.Results);
                    startIndex = (ushort)(dbListResponse.LastIndex + 1);
                }
                catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
                {
                    break;
                }
            }

            PrintResults(context.Console, context.Columns, metadataList);
        }

        private void PrintResults(IConsole console, IEnumerable<string> columnNames, ICollection<DatabaseMetadata> metadataList)
        {
            console.WriteTable(columnNames.Select(n => MetadataColumns.Single(c => c.Header == n)).ToList(), metadataList);
        }
        
        private static readonly IReadOnlyCollection<ConsoleTableColumn<DatabaseMetadata>> MetadataColumns = new[]
        {
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
                GetText = (metadata) => metadata.Version.ToString()
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "ModificationNumber",
                GetText = (metadata) => metadata.ModificationNumber.ToString()
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "CreationDate",
                GetText = (metadata) => metadata.CreationDate.ToString("g")
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "ModificationDate",
                GetText = (metadata) => metadata.CreationDate.ToString("g")
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "LastBackupDate",
                GetText = (metadata) => metadata.CreationDate.ToString("g")
            },
            new ConsoleTableColumn<DatabaseMetadata>
            {
                Header = "Name",
                GetText = (metadata) => metadata.Name
            }
        };
    }
}
