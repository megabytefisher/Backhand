using System;
using System.CommandLine;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;

namespace Backhand.Cli.Commands.UserInfoCommands
{
    public class ReadCommand : SyncFuncCommand
    {
        private class SyncContext
        {
            public required IConsole Console { get; init; }
        }

        public ReadCommand() : base("read", "Reads user info from a device")
        {
            this.SetHandler(async (context) =>
            {
                IConsole console = context.Console;

                Func<DlpConnection, SyncContext> contextFactory = _ => new SyncContext
                {
                    Console = console
                };

                await RunDlpServerAsync(context, SyncAsync, contextFactory).ConfigureAwait(false);
            });
        }
        
        private async Task SyncAsync(DlpConnection connection, SyncContext context, CancellationToken cancellationToken)
        {
            var userInfo = await connection.ReadUserInfoAsync(cancellationToken).ConfigureAwait(false);
            PrintResult(context.Console, userInfo);
        }

        private void PrintResult(IConsole console, ReadUserInfoResponse userInfo)
        {
            StringBuilder sb = new();
            sb.AppendLine($"UserId: {userInfo.UserId}");
            sb.AppendLine($"ViewerId: {userInfo.ViewerId}");
            sb.AppendLine($"LastSyncPcId: {userInfo.LastSyncPcId}");
            sb.AppendLine($"LastSuccessfulSyncDate: {userInfo.LastSuccessfulSyncDate}");
            sb.AppendLine($"LastSyncDate: {userInfo.LastSyncDate}");
            sb.AppendLine($"Username: {userInfo.Username}");
            sb.AppendLine($"Password: {BitConverter.ToString(userInfo.Password)}");

            console.WriteLine(sb.ToString());
        }
    }
}