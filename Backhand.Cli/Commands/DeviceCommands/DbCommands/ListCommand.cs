using Backhand.Cli.Internal.Commands;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Backhand.PalmDb;
using Backhand.PalmDb.Dlp;

namespace Backhand.Cli.Commands.DeviceCommands.DbCommands
{
    public class ListCommand : BaseSyncCommand
    {
        public ListCommand() : base("list", "Lists installed databases on a connected device")
        {
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

            ListSyncHandler syncHandler = new()
            {
                Console = console
            };

            return Task.FromResult(syncHandler);
        }

        private class ListSyncHandler : CommandSyncHandler
        {
            public override async Task OnSyncAsync(CommandSyncContext context, CancellationToken cancellationToken)
            {
                await context.Connection.OpenConduitAsync(cancellationToken).ConfigureAwait(false);
                
                DlpDatabaseRepository deviceDbRepository = new(context.Connection);
                ICollection<PalmDbHeader> deviceDbHeaders =
                    await deviceDbRepository.GetHeadersAsync(cancellationToken);

                PrintResults(context.Console, context.Connection, deviceDbHeaders);
            }
        }

        private static void PrintResults(IAnsiConsole console, DlpConnection connection, IEnumerable<PalmDbHeader> headers)
        {
            Table table = new Table()
                .Title(Markup.Escape($"{connection} Database List"))
                .Expand()
                .AddColumn("Name")
                //.AddColumn("MiscFlags")
                .AddColumn("Attributes")
                .AddColumn("Type")
                .AddColumn("Creator")
                .AddColumn("Version", c => c.RightAligned())
                .AddColumn("ModificationNumber", c => c.RightAligned())
                .AddColumn("CreationDate", c => c.RightAligned())
                .AddColumn("ModificationDate", c => c.RightAligned())
                .AddColumn("LastBackupDate", c => c.RightAligned());

            foreach (PalmDbHeader header in headers)
            {
                table.AddRow(
                    Markup.Escape(header.Name),
                    //Markup.Escape(header.MiscFlags.ToString()),
                    Markup.Escape(header.Attributes.ToString()),
                    Markup.Escape(header.Type),
                    Markup.Escape(header.Creator),
                    Markup.Escape(header.Version.ToString()),
                    Markup.Escape(header.ModificationNumber.ToString()),
                    Markup.Escape(header.CreationDate.ToString("s")),
                    Markup.Escape(header.ModificationDate.ToString("s")),
                    Markup.Escape(header.LastBackupDate.ToString("s"))
                );
            }

            console.Write(table);
        }
    }
}
