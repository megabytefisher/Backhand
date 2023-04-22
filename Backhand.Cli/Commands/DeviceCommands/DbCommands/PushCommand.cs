using Backhand.Cli.Internal.Commands;
using Backhand.Dlp.Commands;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Pdb;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
            Add(FileOption);
            Add(DirectoryOption);

            this.SetHandler(async (context) =>
            {
                PushSyncHandler syncHandler = await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
                await RunDlpServerAsync(context, syncHandler).ConfigureAwait(false);
            });
        }

        public override async Task<ICommandSyncHandler> GetSyncHandlerAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            return await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
        }

        private Task<PushSyncHandler> GetSyncHandlerInternalAsync(InvocationContext context)
        {
            IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            FileInfo? file = context.ParseResult.GetValueForOption(FileOption);
            DirectoryInfo directory = context.ParseResult.GetValueForOption(DirectoryOption)!;

            PushSyncHandler syncHandler = new()
            {
                Console = console,
                File = file,
                Directory = directory
            };

            return Task.FromResult(syncHandler);
        }

        private class PushSyncHandler : CommandSyncHandler
        {
            public required FileInfo? File { get; init; }
            public required DirectoryInfo Directory { get; init; }

            public override async Task OnSyncAsync(CommandSyncContext context, CancellationToken cancellationToken)
            {
                await context.Connection.OpenConduitAsync(cancellationToken).ConfigureAwait(false);

                if (File != null)
                {
                    context.Console.MarkupLineInterpolated($"[grey]Pushing {File.FullName}[/]");
                    await PushDatabaseAsync(context.Connection, File, cancellationToken).ConfigureAwait(false);
                    context.Console.MarkupLineInterpolated($"[green]Wrote database {File.FullName} to device[/]");
                }
                else
                {
                    foreach (FileInfo file in Directory.EnumerateFiles("*.pdb", SearchOption.TopDirectoryOnly))
                    {
                        context.Console.MarkupLineInterpolated($"[grey]Pushing {file.FullName}[/]");
                        await PushDatabaseAsync(context.Connection, file, cancellationToken).ConfigureAwait(false);
                        context.Console.MarkupLineInterpolated($"[green]Wrote database {file.FullName} to device[/]");
                    }

                    foreach (FileInfo file in Directory.EnumerateFiles("*.prc", SearchOption.TopDirectoryOnly))
                    {
                        context.Console.MarkupLineInterpolated($"[grey]Pushing {file.FullName}[/]");
                        await PushDatabaseAsync(context.Connection, file, cancellationToken).ConfigureAwait(false);
                        context.Console.MarkupLineInterpolated($"[green]Wrote database {file.FullName} to device[/]");
                    }
                }
            }

            public async Task PushDatabaseAsync(DlpConnection connection, FileInfo file, CancellationToken cancellationToken)
            {
                await using var fileStream = file.OpenRead();
                Database fileDb =
                    file.Extension.ToLower() == ".prc" ? new ResourceDatabase() :
                    file.Extension.ToLower() == ".pdb" ? new RecordDatabase() :
                    null!;

                await using (FileStream dbStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await fileDb.DeserializeAsync(dbStream, cancellationToken).ConfigureAwait(false);
                }

                CreateDbResponse createDbResult = await connection.CreateDbAsync(new()
                {
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
                    await connection.WriteAppBlockAsync(new()
                    {
                        DbHandle = dbHandle,
                        Data = fileDb.AppInfo
                    }, cancellationToken).ConfigureAwait(false);
                }

                // Write SortInfo
                if (fileDb.SortInfo is { Length: > 0 })
                {
                    await connection.WriteSortBlockAsync(new()
                    {
                        DbHandle = dbHandle,
                        Data = fileDb.SortInfo
                    }, cancellationToken).ConfigureAwait(false);
                }

                switch (fileDb)
                {
                    // Write entries
                    case ResourceDatabase resourceDb:
                    {
                        foreach (DatabaseResource resource in resourceDb.Resources)
                        {
                            await connection.WriteResourceAsync(new()
                            {
                                DbHandle = dbHandle,
                                Type = resource.Type,
                                ResourceId = resource.ResourceId,
                                Data = resource.Data
                            }, cancellationToken).ConfigureAwait(false);
                        }

                        break;
                    }
                    case RecordDatabase recordDb:
                    {
                        foreach (RawDatabaseRecord record in recordDb.Records)
                        {
                            await connection.WriteRecordAsync(new()
                            {
                                DbHandle = dbHandle,
                                RecordId = record.UniqueId,
                                Attributes = (DlpRecordAttributes)record.Attributes,
                                Category = record.Category,
                                Data = record.Data
                            }, cancellationToken).ConfigureAwait(false);
                        }

                        break;
                    }
                    default:
                        throw new NotImplementedException();
                }

                await connection.CloseDbAsync(new()
                {
                    DbHandle = dbHandle
                }, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}