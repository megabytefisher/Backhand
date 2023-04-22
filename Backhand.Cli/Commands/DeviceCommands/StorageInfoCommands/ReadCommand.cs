using Backhand.Cli.Internal.Commands;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StorageInfo = Backhand.Dlp.Commands.v1_0.Arguments.ReadStorageInfoMainResponse.StorageInfo;

namespace Backhand.Cli.Commands.DeviceCommands.StorageInfoCommands
{
    public class ReadCommand : BaseSyncCommand
    {
        public ReadCommand()
            : base("read", "Reads storage info from a device")
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
                List<StorageInfo> infos = new();

                await context.Connection.OpenConduitAsync().ConfigureAwait(false);

                byte cardNo = 0;
                byte lastCard = 0;
                do
                {
                    try
                    {
                        (var mainInfo, _) = await context.Connection.ReadStorageInfoAsync(new()
                        {
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

                PrintResults(context.Console, context.Connection, infos.ToList());
            }
        }

        private static void PrintResults(IAnsiConsole console, DlpConnection connection, IEnumerable<StorageInfo> infos)
        {
            Table table = new Table()
                .Title(Markup.Escape($"{connection} Storage Info"))
                .Expand()
                .AddColumn("CardNo")
                .AddColumn("CardVersion")
                .AddColumn("CardDate")
                .AddColumn("RomSize")
                .AddColumn("RamSize")
                .AddColumn("FreeRam")
                .AddColumn("CardName")
                .AddColumn("ManufacturerName");

            foreach (StorageInfo info in infos)
            {
                table.AddRow(
                    Markup.Escape(info.CardNo.ToString()),
                    Markup.Escape(info.CardVersion.ToString()),
                    Markup.Escape(info.CardDate.ToString("s")),
                    Markup.Escape(info.RomSize.ToString()),
                    Markup.Escape(info.RamSize.ToString()),
                    Markup.Escape(info.FreeRam.ToString()),
                    Markup.Escape(info.CardName),
                    Markup.Escape(info.ManufacturerName)
                );
            }

            console.Write(table);
        }
    }
}