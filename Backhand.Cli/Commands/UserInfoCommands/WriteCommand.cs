using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Protocols.Dlp;

namespace Backhand.Cli.Commands.UserInfoCommands
{
    public class WriteCommand : SyncFuncCommand
    {
        private class SyncContext
        {
            public required uint? UserId { get; init; }
            public required uint? ViewerId { get; init; }
            public required uint? LastSyncPcId { get; init; }
            public required DateTime? LastSuccessfulSyncDate { get; init; }
            public required string? Username { get; init; }
            public required IConsole Console { get; init; }
        }

        private static readonly Option<uint> UserIdOption = new("--userid", "The user id to write to the device");
        private static readonly Option<uint> ViewerIdOption = new("--viewerid", "The viewer id to write to the device");
        private static readonly Option<uint> LastSyncPcIdOption = new("--lastsyncpcid", "The last sync pc id to write to the device");
        private static readonly Option<DateTime> LastSuccessfulSyncDateOption = new("--lastsuccessfullsyncdate", "The last successful sync date to write to the device");
        private static readonly Option<string> UsernameOption = new("--username", "The username to write to the device");

        public WriteCommand() : base("write", "Writes user info to a device")
        {
            this.AddOption(UserIdOption);
            this.AddOption(ViewerIdOption);
            this.AddOption(LastSyncPcIdOption);
            this.AddOption(LastSuccessfulSyncDateOption);
            this.AddOption(UsernameOption);

            this.SetHandler(async (context) =>
            {
                uint? userId = context.ParseResult.GetValueForOption(UserIdOption);
                uint? viewerId = context.ParseResult.GetValueForOption(ViewerIdOption);
                uint? lastSyncPcId = context.ParseResult.GetValueForOption(LastSyncPcIdOption);
                DateTime? lastSuccessfulSyncDate = context.ParseResult.GetValueForOption(LastSuccessfulSyncDateOption);
                string? username = context.ParseResult.GetValueForOption(UsernameOption);

                IConsole console = context.Console;

                Func<DlpConnection, SyncContext> contextFactory = _ => new SyncContext
                {
                    UserId = userId,
                    ViewerId = viewerId,
                    LastSyncPcId = lastSyncPcId,
                    LastSuccessfulSyncDate = lastSuccessfulSyncDate,
                    Username = username,
                    Console = console
                };

                await RunDlpServerAsync(context, SyncAsync, contextFactory).ConfigureAwait(false);
            });
        }
        
        private async Task SyncAsync(DlpConnection connection, SyncContext context, CancellationToken cancellationToken)
        {
            var userInfo = await connection.ReadUserInfoAsync(cancellationToken).ConfigureAwait(false);

            await connection.WriteUserInfoAsync(new() {
                UserId = context.UserId ?? userInfo.UserId,
                ViewerId = context.ViewerId ?? userInfo.ViewerId,
                LastSyncPcId = context.LastSyncPcId ?? userInfo.LastSyncPcId,
                LastSuccessfulSyncDate = context.LastSuccessfulSyncDate ?? userInfo.LastSuccessfulSyncDate,
                Username = context.Username ?? userInfo.Username
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}