using System;
using System.CommandLine;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;

namespace Backhand.Cli.Commands.SysInfoCommands
{
    public class ReadCommand : SyncFuncCommand
    {
        private class SyncContext
        {
            public required IConsole Console { get; init; }
        }

        public ReadCommand() : base("read", "Reads system info from a device")
        {
            this.SetHandler(async (context) =>
            {
                IConsole console = context.Console;

                Func<DlpConnection, SyncContext> contextFactory = _ => new()
                {
                    Console = console
                };

                await RunDlpServerAsync(context, SyncAsync, contextFactory).ConfigureAwait(false);
            });
        }
        
        private async Task SyncAsync(DlpConnection connection, SyncContext context, CancellationToken cancellationToken)
        {
            (var sysInfo, var dlpInfo) = await connection.ReadSysInfoAsync(new() {
                HostDlpVersionMajor = 1,
                HostDlpVersionMinor = 1
            }, cancellationToken).ConfigureAwait(false);

            PrintResult(context.Console, sysInfo, dlpInfo);
        }

        private void PrintResult(IConsole console, ReadSysInfoSystemResponse systemInfo, ReadSysInfoDlpResponse dlpInfo)
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