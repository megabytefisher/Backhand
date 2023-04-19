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

namespace Backhand.Cli.Commands.DeviceCommands.SysInfoCommands
{
    public class ReadCommand : BaseSyncCommand
    {
        public ReadCommand() : base("read", "Reads system info from a device")
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
                (var sysInfo, var dlpInfo) = await context.Connection.ReadSysInfoAsync(new() {
                    HostDlpVersionMajor = 1,
                    HostDlpVersionMinor = 1
                }, cancellationToken).ConfigureAwait(false);

                PrintResult(context.Console, sysInfo, dlpInfo);
            }
        }

        private static void PrintResult(IConsole console, ReadSysInfoSystemResponse systemInfo, ReadSysInfoDlpResponse dlpInfo)
        {
            StringBuilder sb = new();
            sb.AppendLine($"RomVersion: {systemInfo.RomVersion}");
            sb.AppendLine($"Locale: {systemInfo.Locale}");
            sb.AppendLine($"ProductId: {BitConverter.ToString(systemInfo.ProductId)}");
            sb.AppendLine($"ClientDlpVersionMajor: {dlpInfo.ClientDlpVersionMajor}");
            sb.AppendLine($"ClientDlpVersionMinor: {dlpInfo.ClientDlpVersionMinor}");
            sb.AppendLine($"MinimumDlpVersionMajor: {dlpInfo.MinimumDlpVersionMajor}");
            sb.AppendLine($"MinimumDlpVersionMinor: {dlpInfo.MinimumDlpVersionMinor}");
            sb.AppendLine($"MaxRecordSize: {dlpInfo.MaxRecordSize}");

            console.WriteLine(sb.ToString());
        }
    }
}