using System;
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

namespace Backhand.Cli.Commands.DbCommands
{
    public class PushCommand : SyncFuncCommand
    {
        public static readonly Option<FileInfo> FileOption = new(new[] { "--file", "-f" }, "The file to push to the device.")
        {
            IsRequired = true
        };

        public PushCommand()
            : base("push", "Push a database file to the device.")
        {
            AddOption(FileOption);

            this.SetHandler(async (context) =>
            {
                FileInfo file = context.ParseResult.GetValueForOption(FileOption)!;

                IConsole console = context.Console;

                DlpSyncFunc syncFunc = (c, ct) => SyncAsync(c, file, console, ct);

                await RunDlpServerAsync(context, syncFunc).ConfigureAwait(false);
                console.WriteLine("Operation completed successfully.");
            });
        }

        private async Task SyncAsync(DlpConnection connection, FileInfo file, IConsole console, CancellationToken cancellationToken)
        {
            await connection.OpenConduitAsync().ConfigureAwait(false);

            using var fileStream = file.OpenRead();
            Database fileDb =
                file.Extension == ".prc" ? new ResourceDatabase() :
                file.Extension == ".pdb" ? new RecordDatabase() :
                null!;

            using (FileStream dbStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await fileDb.DeserializeAsync(dbStream, cancellationToken).ConfigureAwait(false);
            }

            CreateDbResponse createDbResult = await connection.CreateDbAsync(new() {
                Creator = fileDb.Creator,
                Type = fileDb.Type,
                CardId = 0,
                Attributes = (DlpDatabaseAttributes)fileDb.Attributes,
                Version = fileDb.Version,
                Name = fileDb.Name,
            }, cancellationToken).ConfigureAwait(false);

            byte dbHandle = createDbResult.DbHandle;

            // Write AppInfo
            if (fileDb.AppInfo is { Length: > 0 })
            {
                await connection.WriteAppBlockAsync(new() {
                    DbHandle = dbHandle,
                    Data = fileDb.AppInfo
                }, cancellationToken).ConfigureAwait(false);
            }

            // Write SortInfo
            if (fileDb.SortInfo is { Length: > 0 })
            {
                await connection.WriteSortBlockAsync(new() {
                    DbHandle = dbHandle,
                    Data = fileDb.SortInfo
                }, cancellationToken).ConfigureAwait(false);
            }

            // Write entries
            if (fileDb is ResourceDatabase resourceDb)
            {
                foreach (DatabaseResource resource in resourceDb.Resources)
                {
                    await connection.WriteResourceAsync(new() {
                        DbHandle = dbHandle,
                        Type = resource.Type,
                        ResourceId = resource.ResourceId,
                        Data = resource.Data
                    }, cancellationToken).ConfigureAwait(false);
                }
            }
            else if (fileDb is RecordDatabase recordDb)
            {
                foreach (DatabaseRecord record in recordDb.Records)
                {
                    await connection.WriteRecordAsync(new() {
                        DbHandle = dbHandle,
                        RecordId = record.UniqueId,
                        Attributes = (DlpRecordAttributes)record.Attributes,
                        Category = record.Category,
                        Data = record.Data
                    }, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            await connection.CloseDbAsync(new() {
                DbHandle = dbHandle
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}