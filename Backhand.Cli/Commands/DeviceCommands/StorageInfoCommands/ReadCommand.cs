using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Cli.Internal;
using Backhand.Dlp;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;

namespace Backhand.Cli.Commands.DeviceCommands.StorageInfoCommands
{
    public class ReadCommand : BaseSyncCommand
    {
        public readonly Option<IEnumerable<string>> ColumnsOption =
            new Option<IEnumerable<string>>(new[] { "--columns", "-c" }, () => new[] { "CardNo", "CardName", "RomSize", "RamSize", "FreeRam" })
            {
                AllowMultipleArgumentsPerToken = true
            }.FromAmong(StorageInfoColumns.Select(c => c.Header).ToArray());

        public ReadCommand()
            : base("read", "Reads storage info from a device")
        {
            AddOption(ColumnsOption);

            this.SetHandler(async (context) =>
            {
                IEnumerable<string> columnNames = context.ParseResult.GetValueForOption<IEnumerable<string>>(ColumnsOption)!;

                IConsole console = context.Console;

                ReadSyncHandler syncHandler = new()
                {
                    Columns = columnNames,
                    Console = console
                };

                await RunDlpServerAsync<ReadSyncContext>(context, syncHandler).ConfigureAwait(false);
            });
        }

        private class ReadSyncContext
        {
            public required DlpConnection Connection { get; init; }
            public required IEnumerable<string> Columns { get; init; }
            public required IConsole Console { get; init; }
        }

        private class ReadSyncHandler : ISyncHandler<ReadSyncContext>
        {
            public required IEnumerable<string> Columns { get; init; }
            public required IConsole Console { get; init; }

            public Task<ReadSyncContext> InitializeAsync(DlpConnection connection, CancellationToken cancellationToken)
            {
                return Task.FromResult(new ReadSyncContext
                {
                    Connection = connection,
                    Columns = Columns,
                    Console = Console
                });
            }

            public async Task OnSyncAsync(ReadSyncContext context, CancellationToken cancellationToken)
            {
                List<ReadStorageInfoMainResponse.StorageInfo> infos = new List<ReadStorageInfoMainResponse.StorageInfo>();

                await context.Connection.OpenConduitAsync().ConfigureAwait(false);

                byte cardNo = 0;
                byte lastCard = 0;
                do
                {
                    try
                    {
                        (var mainInfo, var extInfo) = await context.Connection.ReadStorageInfoAsync(new() {
                            CardNo = cardNo
                        }, cancellationToken).ConfigureAwait(false);

                        lastCard = mainInfo.LastCard;
                        infos.AddRange(mainInfo.Results);
                    }
                    catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
                    {
                        // Ignore
                    }

                    cardNo++;
                } while (cardNo < lastCard);

                PrintResults(context.Console, context.Columns, infos.ToList());
            }
        }

        private static void PrintResults(IConsole console, IEnumerable<string> columnNames, ICollection<ReadStorageInfoMainResponse.StorageInfo> infos)
        {
            console.WriteTable(columnNames.Select(n => StorageInfoColumns.Single(c => c.Header == n)).ToList(), infos);
        }
        
        private static readonly IReadOnlyCollection<ConsoleTableColumn<ReadStorageInfoMainResponse.StorageInfo>> StorageInfoColumns = new[]
        {
            new ConsoleTableColumn<ReadStorageInfoMainResponse.StorageInfo>
            {
                Header = "CardNo",
                GetText = (info) => info.CardNo.ToString()
            },
            new ConsoleTableColumn<ReadStorageInfoMainResponse.StorageInfo>
            {
                Header = "CardVersion",
                GetText = (info) => info.CardVersion.ToString()
            },
            new ConsoleTableColumn<ReadStorageInfoMainResponse.StorageInfo>
            {
                Header = "CardDate",
                GetText = (info) => info.CardDate.ToString()
            },
            new ConsoleTableColumn<ReadStorageInfoMainResponse.StorageInfo>
            {
                Header = "RomSize",
                GetText = (info) => info.RomSize.ToString()
            },
            new ConsoleTableColumn<ReadStorageInfoMainResponse.StorageInfo>
            {
                Header = "RamSize",
                GetText = (info) => info.RamSize.ToString()
            },
            new ConsoleTableColumn<ReadStorageInfoMainResponse.StorageInfo>
            {
                Header = "FreeRam",
                GetText = (info) => info.FreeRam.ToString()
            },
            new ConsoleTableColumn<ReadStorageInfoMainResponse.StorageInfo>
            {
                Header = "CardName",
                GetText = (info) => info.CardName
            },
            new ConsoleTableColumn<ReadStorageInfoMainResponse.StorageInfo>
            {
                Header = "ManufacturerName",
                GetText = (info) => info.ManufacturerName
            }
        };
    }
}