using Backhand.Cli.Internal.Commands;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands.v1_0.Arguments;

namespace Backhand.Cli.Commands.DeviceCommands.UserInfoCommands
{
    public class WriteCommand : BaseSyncCommand
    {
        private static readonly Option<uint?> UserIdOption = new("--userid", "The user id to write to the device");
        private static readonly Option<uint?> ViewerIdOption = new("--viewerid", "The viewer id to write to the device");
        private static readonly Option<uint?> LastSyncPcIdOption = new("--lastsyncpcid", "The last sync pc id to write to the device");
        private static readonly Option<DateTime?> LastSuccessfulSyncDateOption = new("--lastsuccessfulsyncdate", "The last successful sync date to write to the device");
        private static readonly Option<string?> UsernameOption = new("--username", "The username to write to the device");

        public WriteCommand() : base("write", "Writes user info to a device")
        {
            Add(UserIdOption);
            Add(ViewerIdOption);
            Add(LastSyncPcIdOption);
            Add(LastSuccessfulSyncDateOption);
            Add(UsernameOption);

            this.SetHandler(async (context) =>
            {
                WriteSyncHandler syncHandler = await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
                await RunDlpServerAsync(context, syncHandler).ConfigureAwait(false);
            });
        }

        public override async Task<ICommandSyncHandler> GetSyncHandlerAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            return await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
        }

        private Task<WriteSyncHandler> GetSyncHandlerInternalAsync(InvocationContext context)
        {
            IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            uint? userId = context.ParseResult.GetValueForOption(UserIdOption);
            uint? viewerId = context.ParseResult.GetValueForOption(ViewerIdOption);
            uint? lastSyncPcId = context.ParseResult.GetValueForOption(LastSyncPcIdOption);
            DateTime? lastSuccessfulSyncDate = context.ParseResult.GetValueForOption(LastSuccessfulSyncDateOption);
            string? username = context.ParseResult.GetValueForOption(UsernameOption);

            WriteSyncHandler syncHandler = new()
            {
                UserId = userId,
                ViewerId = viewerId,
                LastSyncPcId = lastSyncPcId,
                LastSuccessfulSyncDate = lastSuccessfulSyncDate,
                Username = username,
                Console = console
            };

            return Task.FromResult(syncHandler);
        }

        private class WriteSyncHandler : CommandSyncHandler
        {
            public required uint? UserId { get; init; }
            public required uint? ViewerId { get; init; }
            public required uint? LastSyncPcId { get; init; }
            public required DateTime? LastSuccessfulSyncDate { get; init; }
            public required string? Username { get; init; }

            public override async Task OnSyncAsync(CommandSyncContext context, CancellationToken cancellationToken)
            {
                var userInfo = await context.Connection.ReadUserInfoAsync(cancellationToken).ConfigureAwait(false);

                WriteUserInfoRequest request = new()
                {
                    UserId = UserId ?? userInfo.UserId,
                    ViewerId = ViewerId ?? userInfo.ViewerId,
                    LastSyncPcId = LastSyncPcId ?? userInfo.LastSyncPcId,
                    LastSyncDate = LastSuccessfulSyncDate ?? userInfo.LastSuccessfulSyncDate,
                    Username = Username ?? userInfo.Username
                };
                
                PrintRequest(context.Console, context.Connection, request);
                await context.Connection.WriteUserInfoAsync(request, cancellationToken).ConfigureAwait(false);
                
                context.Console.MarkupLineInterpolated($"[green]Successfully wrote user info to device[/]");
            }
        }

        private static void PrintRequest(IAnsiConsole console, DlpConnection connection, WriteUserInfoRequest userInfo)
        {
            Table table = new Table()
                .Title(new TableTitle(Markup.Escape($"{connection} New User Information")))
                .Expand()
                .AddColumn("Name")
                .AddColumn("Value")
                .AddRow("UserId", Markup.Escape(userInfo.UserId.ToString()))
                .AddRow("ViewerId", Markup.Escape(userInfo.ViewerId.ToString()))
                .AddRow("LastSyncPcId", Markup.Escape(userInfo.LastSyncPcId.ToString()))
                .AddRow("LastSuccessfulSyncDate", Markup.Escape(userInfo.LastSyncDate.ToString("s")))
                .AddRow("LastSyncDate", Markup.Escape(userInfo.LastSyncDate.ToString("s")))
                .AddRow("Username", Markup.Escape(userInfo.Username));

            console.Write(table);
        }
    }
}