using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp;
using Backhand.Dlp.Commands;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Pdb;
using Backhand.Protocols.Dlp;
using static Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListRequest;
using static Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListResponse;

namespace Backhand.Cli.Commands.DbCommands
{
    public class SyncCommand : SyncFuncCommand
    {
        private class SyncContext
        {
            public required IConsole Console { get; set; }
        }

        public SyncCommand()
            : base("sync", "Sync a device's databases with a local directory.")
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
            await connection.OpenConduitAsync().ConfigureAwait(false);

            List<DatabaseMetadata> databaseList = new List<DatabaseMetadata>();
            ushort startIndex = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    ReadDbListResponse dbListResponse = await connection.ReadDbListAsync(new()
                    {
                        Mode = ReadDbListMode.ListMultiple | ReadDbListMode.ListRam,
                        CardId = 0,
                        StartIndex = startIndex
                    }, cancellationToken).ConfigureAwait(false);

                    databaseList.AddRange(dbListResponse.Results);
                    startIndex = (ushort)(dbListResponse.LastIndex + 1);
                }
                catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
                {
                    break;
                }
            }

            foreach (DatabaseMetadata databaseMetadata in databaseList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string databaseName = databaseMetadata.Name;

                
            }
        }
    }
}