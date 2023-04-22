using Backhand.Cli.Internal.Commands;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands.DeviceCommands.SysInfoCommands
{
    public class ReadCommand : BaseSyncCommand
    {
        public ReadCommand() : base("read", "Reads system info from a device")
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
                (ReadSysInfoSystemResponse sysInfo, ReadSysInfoDlpResponse dlpInfo) =
                    await context.Connection.ReadSysInfoAsync(new()
                    {
                        HostDlpVersionMajor = 1,
                        HostDlpVersionMinor = 1
                    }, cancellationToken).ConfigureAwait(false);

                PrintResult(context.Console, context.Connection, sysInfo, dlpInfo);
            }
        }

        private static void PrintResult(IAnsiConsole console, DlpConnection connection, ReadSysInfoSystemResponse systemInfo, ReadSysInfoDlpResponse dlpInfo)
        {
            Table table = new Table()
                .Title(Markup.Escape($"{connection} System Info"))
                .Expand()
                .AddColumn("Name")
                .AddColumn("Value")
                .AddRow("RomVersion", Markup.Escape(systemInfo.RomVersion.ToString()))
                .AddRow("Locale", Markup.Escape(systemInfo.Locale.ToString()))
                .AddRow("ProductId", BitConverter.ToString(systemInfo.ProductId))
                .AddRow("ClientDlpVersionMajor", Markup.Escape(dlpInfo.ClientDlpVersionMajor.ToString()))
                .AddRow("ClientDlpVersionMinor", Markup.Escape(dlpInfo.ClientDlpVersionMinor.ToString()))
                .AddRow("MinimumDlpVersionMajor", Markup.Escape(dlpInfo.MinimumDlpVersionMajor.ToString()))
                .AddRow("MinimumDlpVersionMinor", Markup.Escape(dlpInfo.MinimumDlpVersionMinor.ToString()))
                .AddRow("MaxRecordSize", Markup.Escape(dlpInfo.MaxRecordSize.ToString()));

            console.Write(table);
        }
    }
}