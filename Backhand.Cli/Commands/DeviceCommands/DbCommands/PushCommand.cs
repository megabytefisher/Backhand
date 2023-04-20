using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Cli.Internal;
using Backhand.Dlp;
using Backhand.Dlp.Commands;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Pdb;
using Backhand.Protocols.Dlp;

namespace Backhand.Cli.Commands.DeviceCommands.DbCommands
{
    public class PushCommand : BaseSyncCommand
    {
        private static readonly Option<FileInfo?> FileOption =
            new(new[] { "--file", "-f" }, "The file to push to the device.");

        private static readonly Option<DirectoryInfo> DirectoryOption =
            new(new[] { "--path", "-p" }, () => new DirectoryInfo("."), "The directory to push to the device.");

        public PushCommand()
            : base("push", "Push a database file to the device.")
        {
            AddOption(FileOption);
            AddOption(DirectoryOption);

            this.SetHandler(async (context) =>
            {
                FileInfo? file = context.ParseResult.GetValueForOption(FileOption)!;
                DirectoryInfo directory = context.ParseResult.GetValueForOption(DirectoryOption)!;

                IConsole console = context.Console;

                PushSyncHandler syncHandler = new()
                {
                    File = file,
                    Directory = directory,
                    Console = console
                };

                await RunDlpServerAsync(context, syncHandler).ConfigureAwait(false);
            });
        }

        private class PushSyncContext
        {
            public required DlpConnection Connection { get; init; }
            public required FileInfo? File { get; init; }
            public required DirectoryInfo Directory { get; init; }
            public required IConsole Console { get; init; }
        }

        private class PushSyncHandler : ISyncHandler<PushSyncContext>
        {
            public required FileInfo? File { get; init; }
            public required IConsole Console { get; init; }
            public required DirectoryInfo Directory { get; init; }

            public Task<PushSyncContext> InitializeAsync(DlpConnection connection, CancellationToken cancellationToken)
            {
                return Task.FromResult(new PushSyncContext {
                    Connection = connection,
                    File = File,
                    Directory = Directory,
                    Console = Console
                });
            }

            public async Task OnSyncAsync(PushSyncContext context, CancellationToken cancellationToken)
            {
                await context.Connection.OpenConduitAsync(cancellationToken).ConfigureAwait(false);

                if (context.File != null)
                {
                    await PushDatabaseAsync(context.Connection, context.File, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    foreach (FileInfo file in context.Directory.EnumerateFiles("*.pdb", SearchOption.TopDirectoryOnly))
                    {
                        await PushDatabaseAsync(context.Connection, file, cancellationToken).ConfigureAwait(false);
                    }

                    foreach (FileInfo file in context.Directory.EnumerateFiles("*.prc", SearchOption.TopDirectoryOnly))
                    {
                        await PushDatabaseAsync(context.Connection, file, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            public async Task PushDatabaseAsync(DlpConnection connection, FileInfo file, CancellationToken cancellationToken)
            {
                using var fileStream = file.OpenRead();
                Database fileDb =
                    file.Extension.ToLower() == ".prc" ? new ResourceDatabase() :
                    file.Extension.ToLower() == ".pdb" ? new RecordDatabase() :
                    null!;

                using (FileStream dbStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await fileDb.DeserializeAsync(dbStream, cancellationToken).ConfigureAwait(false);
                }

                try
                {
                    await connection.DeleteDbAsync(new() {
                        Name = fileDb.Name
                    }, cancellationToken).ConfigureAwait(false);
                } catch { }

                CreateDbResponse createDbResult = await connection.CreateDbAsync(new() {
                    Creator = fileDb.Creator,
                    Type = fileDb.Type,
                    CardId = 0,
                    Attributes = (DlpDatabaseAttributes)((int)fileDb.Attributes & ~(int)DlpDatabaseAttributes.ReadOnly),
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
                    foreach (RawDatabaseRecord record in recordDb.Records)
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
}