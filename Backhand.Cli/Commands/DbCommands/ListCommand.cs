using Backhand.Cli.Internal;
using Backhand.Dlp;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        public static readonly Option<IEnumerable<ReadDbListMode>> ReadModesOption = new(new[] { "--read-modes", "-m" }, () => new[] { ReadDbListMode.ListRam | ReadDbListMode.ListMultiple })
        {
            AllowMultipleArgumentsPerToken = true
        };

        public static readonly Option<bool> ServerMode = new(new[] { "--server-mode", "-s" }, () => false);

        public static readonly Option<IEnumerable<string>> ColumnsOption = new Option<IEnumerable<string>>(new[] { "--columns", "-c" }, () => new[] { "Name", "Attributes", "Type", "Creator" })
        {
            AllowMultipleArgumentsPerToken = true
        }.FromAmong(MetadataColumns.Select(c => c.Header).ToArray());

        public ListCommand() : base("list", "Lists databases on a device")
        {
            this.Add(ReadModesOption);
            this.Add(ColumnsOption);
            this.Add(ServerMode);

            this.SetHandler(async (context) =>
            {
                IEnumerable<ReadDbListMode> readModes = context.ParseResult.GetValueForOption(ReadModesOption)!;
                IEnumerable<string> columns = context.ParseResult.GetValueForOption(ColumnsOption)!;
                bool serverMode = context.ParseResult.GetValueForOption(ServerMode)!;
                string serialPortName = context.ParseResult.GetValueForOption(SerialPortNameOption)!;

                IConsole console = context.Console;
                ILogger logger = context.BindingContext.GetRequiredService<ILogger>();
                CancellationToken cancellationToken = context.GetCancellationToken();

                await RunCommandAsync(readModes, columns, serverMode, serialPortName, console, logger, cancellationToken).ConfigureAwait(false);
            });
        }

        private async Task RunCommandAsync(IEnumerable<ReadDbListMode> readModes, IEnumerable<string> columns, bool serverMode, string serialPortName, IConsole console, ILogger logger, CancellationToken cancellationToken)
        {
            ReadDbListMode readMode = readModes.Aggregate(ReadDbListMode.None, (acc, cur) => acc | cur);

            async Task SyncFunc(DlpConnection connection, CancellationToken cancellationToken)
            {
                await connection.OpenConduitAsync(cancellationToken);

                List<DatabaseMetadata> metadataList = new List<DatabaseMetadata>();
                ushort startIndex = 0;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ReadDbListResponse dbListResponse = await connection.ReadDbListAsync(new()
                    {
                        Mode = readMode,
                        CardId = 0,
                        StartIndex = startIndex
                    }, cancellationToken);

                    metadataList.AddRange(dbListResponse.Results);
                    startIndex = (ushort)(dbListResponse.LastIndex + 1);

                    if (dbListResponse.LastIndex == dbListResponse.Results.Last().Index)
                    {
                        break;
                    }
                }

                PrintResults(console, columns, metadataList);
            }

            SerialDlpServer dlpServer = new SerialDlpServer(SyncFunc, serialPortName, singleSync: !serverMode);
            await dlpServer.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        private void PrintResults(IConsole console, IEnumerable<string> columnNames, ICollection<DatabaseMetadata> metadataList)
        {
            console.WriteTable(columnNames.Select(n => MetadataColumns.Single(c => c.Header == n)).ToList(), metadataList);
        }
    }
}
