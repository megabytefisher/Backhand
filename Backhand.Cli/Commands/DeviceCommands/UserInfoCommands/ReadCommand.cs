using Backhand.Cli.Internal.Commands;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands.v1_0;

namespace Backhand.Cli.Commands.DeviceCommands.UserInfoCommands
{
    public class ReadCommand : BaseSyncCommand
    {
        public ReadCommand()
            : base("read", "Reads user info from a device")
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
                ReadUserInfoResponse userInfo = await context.Client.ReadUserInfoAsync(cancellationToken).ConfigureAwait(false);
                PrintResult(context.Console, context.Client, userInfo);
            }
        }

        private static void PrintResult(IAnsiConsole console, DlpClient client, ReadUserInfoResponse userInfo)
        {
            Table table = new Table()
                .Title(Markup.Escape($"{client} User Information"))
                .Expand()
                .AddColumn("Name")
                .AddColumn("Value")
                .AddRow("UserId", Markup.Escape(userInfo.UserId.ToString()))
                .AddRow("ViewerId", Markup.Escape(userInfo.ViewerId.ToString()))
                .AddRow("LastSyncPcId", Markup.Escape(userInfo.LastSyncPcId.ToString()))
                .AddRow("LastSuccessfulSyncDate", Markup.Escape(userInfo.LastSuccessfulSyncDate.ToString("s")))
                .AddRow("LastSyncDate", Markup.Escape(userInfo.LastSyncDate.ToString("s")))
                .AddRow("Username", Markup.Escape(userInfo.Username))
                .AddRow("Password", Markup.Escape(BitConverter.ToString(userInfo.Password)));

            console.Write(table);
        }
    }
}