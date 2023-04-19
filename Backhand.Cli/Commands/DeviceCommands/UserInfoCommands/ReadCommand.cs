using System;
using System.CommandLine;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Cli.Internal;
using Backhand.Dlp;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;

namespace Backhand.Cli.Commands.DeviceCommands.UserInfoCommands
{
    public class ReadCommand : BaseSyncCommand
    {
        public ReadCommand()
            : base("read", "Reads user info from a device")
        {
            this.SetHandler(async (context) =>
            {
                IConsole console = context.Console;

                ReadSyncHandler syncHandler = new()
                {
                    Console = console
                };

                await RunDlpServerAsync<ReadSyncContext>(context, syncHandler).ConfigureAwait(false);
            });
        }

        private class ReadSyncContext
        {
            public required DlpConnection Connection { get; init; }
            public required IConsole Console { get; init; }
        }

        private class ReadSyncHandler : ISyncHandler<ReadSyncContext>
        {
            public required IConsole Console { get; init; }

            public Task<ReadSyncContext> InitializeAsync(DlpConnection connection, CancellationToken cancellationToken)
            {
                return Task.FromResult(new ReadSyncContext
                {
                    Connection = connection,
                    Console = Console
                });
            }

            public async Task OnSyncAsync(ReadSyncContext context, CancellationToken cancellationToken)
            {
                var userInfo = await context.Connection.ReadUserInfoAsync(cancellationToken).ConfigureAwait(false);
                PrintResult(context.Console, userInfo);
            }
        }

        private static void PrintResult(IConsole console, ReadUserInfoResponse userInfo)
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