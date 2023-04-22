using Backhand.Cli.Internal.Commands;
using Backhand.Dlp.Commands;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Pdb;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseMetadata = Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListResponse.DatabaseMetadata;
using OpenDbMode = Backhand.Dlp.Commands.v1_0.Arguments.OpenDbRequest.OpenDbMode;
using ReadDbListMode = Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListRequest.ReadDbListMode;

namespace Backhand.Cli.Commands.DeviceCommands.DbCommands
{
    public class PullCommand : BaseSyncCommand
    {
        private static readonly Option<bool> RomOption = new(new[] { "--rom", "-r" }, "Pulls databases from ROM rather than RAM.");

        private static readonly Option<IEnumerable<DlpDatabaseAttributes>> AttributesOption =
            new(new[] { "--attributes", "-a" }, () => new[] { DlpDatabaseAttributes.Backup }, "Only pulls databases with the specified attribute(s).");

        private static readonly Argument<string[]> NamesArgument =
            new("names", "Names of databases to pull. If not specified, all available databases will be pulled.");

        public PullCommand()
            : base("pull", "Pull a database file from the device.")
        {
            Add(RomOption);
            Add(AttributesOption);
            Add(NamesArgument);

            this.SetHandler(async (context) =>
            {
                PullSyncHandler syncHandler = await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
                await RunDlpServerAsync(context, syncHandler).ConfigureAwait(false);
            });
        }

        public override async Task<ICommandSyncHandler> GetSyncHandlerAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            return await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
        }

        private Task<PullSyncHandler> GetSyncHandlerInternalAsync(InvocationContext context)
        {
            IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            bool rom = context.ParseResult.GetValueForOption(RomOption);
            DlpDatabaseAttributes attributes = context.ParseResult.GetValueForOption(AttributesOption)!.Aggregate(DlpDatabaseAttributes.None, (acc, cur) => acc | cur);
            string[]? names = context.ParseResult.GetValueForArgument(NamesArgument);
            names = names.Length == 0 ? null : names;

            PullSyncHandler syncHandler = new()
            {
                Console = console,
                Rom = rom,
                Attributes = attributes,
                Names = names
            };

            return Task.FromResult(syncHandler);
        }

        private class PullSyncHandler : CommandSyncHandler
        {
            public required bool Rom { get; init; }
            public required DlpDatabaseAttributes Attributes { get; init; }
            public required string[]? Names { get; init; }

            public override async Task OnSyncAsync(CommandSyncContext context, CancellationToken cancellationToken)
            {
                await context.Connection.OpenConduitAsync(cancellationToken).ConfigureAwait(false);

                List<DatabaseMetadata> dbResults = new();
                ushort startIndex = 0;
                while (true)
                {
                    try
                    {
                        ReadDbListResponse response = await context.Connection.ReadDbListAsync(new ReadDbListRequest
                        {
                            Mode = ReadDbListMode.ListMultiple | (Rom ? ReadDbListMode.ListRom : ReadDbListMode.ListRam),
                            StartIndex = startIndex
                        }, cancellationToken).ConfigureAwait(false);

                        dbResults.AddRange(response.Results);
                        startIndex = (ushort)(response.LastIndex + 1);
                    }
                    catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
                    {
                        break;
                    }
                }

                if (Names != null)
                {
                    foreach (string name in Names)
                    {
                        DatabaseMetadata? databaseMetadata = dbResults.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                        if (databaseMetadata == null)
                        {
                            context.Console.MarkupLineInterpolated($"[bold red]Database not found: {name}[/]");
                        }
                        else
                        {
                            context.Console.MarkupLineInterpolated($"[grey]Pulling database: {name}[/]");
                            Database database = await PullDatabaseAsync(context.Connection, databaseMetadata, cancellationToken).ConfigureAwait(false);

                            FileInfo outputFile = new(GetFileName(database));

                            await using FileStream outStream = outputFile.OpenWrite();
                            await database.SerializeAsync(outStream, cancellationToken).ConfigureAwait(false);
                            context.Console.MarkupLineInterpolated($"[green]Wrote {outStream.Position} bytes to {outputFile.FullName}[/]");
                        }
                    }
                }
                else
                {
                    IEnumerable<DatabaseMetadata> pullDatabasesEnumerable =
                        Attributes != DlpDatabaseAttributes.None ? dbResults.Where(db => db.Attributes.HasFlag(Attributes)) :
                        dbResults;

                    DatabaseMetadata[] pullDatabases = pullDatabasesEnumerable.ToArray();

                    context.Console.MarkupLineInterpolated($"[grey]Pulling {pullDatabases.Length} databases[/]");

                    foreach (DatabaseMetadata dbMetadata in pullDatabases)
                    {
                        context.Console.MarkupLineInterpolated($"[silver]Pulling database: {dbMetadata.Name}[/]");
                        Database database = await PullDatabaseAsync(context.Connection, dbMetadata, cancellationToken).ConfigureAwait(false);

                        FileInfo outputFile = new(GetFileName(database));

                        await using FileStream outStream = File.OpenWrite(GetFileName(database));
                        await database.SerializeAsync(outStream, cancellationToken).ConfigureAwait(false);
                        context.Console.MarkupLineInterpolated($"[green]Wrote {outStream.Position} bytes to {outputFile.FullName}[/]");
                    }
                }
            }

            private static async Task<Database> PullDatabaseAsync(DlpConnection connection, DatabaseMetadata databaseMetadata, CancellationToken cancellationToken)
            {
                bool isResource = databaseMetadata.Attributes.HasFlag(DlpDatabaseAttributes.ResourceDb);
                Database database = isResource ? new ResourceDatabase() : new RecordDatabase();

                // Fill header info
                database.Name = databaseMetadata.Name;
                database.Attributes = (DatabaseAttributes)databaseMetadata.Attributes;
                database.Version = databaseMetadata.Version;
                database.CreationDate = databaseMetadata.CreationDate;
                database.ModificationDate = databaseMetadata.ModificationDate;
                database.LastBackupDate = databaseMetadata.LastBackupDate;
                database.ModificationNumber = databaseMetadata.ModificationNumber;
                database.Type = databaseMetadata.Type;
                database.Creator = databaseMetadata.Creator;
                database.UniqueIdSeed = 0;

                // Open database on device
                OpenDbResponse openDbResponse = await connection.OpenDbAsync(new()
                {
                    CardId = 0,
                    Name = database.Name,
                    Mode = OpenDbMode.Read
                }, cancellationToken).ConfigureAwait(false);

                byte dbHandle = openDbResponse.DbHandle;

                // Try reading AppInfo from device database
                try
                {
                    ReadAppBlockResponse appInfoResponse = await connection.ReadAppBlockAsync(new()
                    {
                        DbHandle = dbHandle,
                        Length = ushort.MaxValue,
                        Offset = 0
                    }, cancellationToken);

                    database.AppInfo = appInfoResponse.Data;
                }
                catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
                {
                    // No problem
                }

                // Try reading SortInfo from device database
                try
                {
                    ReadSortBlockResponse sortInfoResponse = await connection.ReadSortBlockAsync(new()
                    {
                        DbHandle = dbHandle,
                        Length = ushort.MaxValue,
                        Offset = 0
                    }, cancellationToken);

                    database.SortInfo = sortInfoResponse.Data;
                }
                catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
                {
                    // No problem
                }

                // Fill entries
                if (isResource)
                {
                    await PullResourcesAsync(connection, dbHandle, (ResourceDatabase)database, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await PullRecordsAsync(connection, dbHandle, (RecordDatabase)database, cancellationToken).ConfigureAwait(false);
                }

                // Close database on device
                await connection.CloseDbAsync(new()
                {
                    DbHandle = dbHandle
                }, cancellationToken).ConfigureAwait(false);

                return database;
            }

            private static async Task PullResourcesAsync(DlpConnection connection, byte dbHandle, ResourceDatabase database, CancellationToken cancellationToken)
            {
                for (ushort resourceIndex = 0; ; resourceIndex++)
                {
                    try
                    {
                        ReadResourceByIndexResponse resourceResponse = await connection.ReadResourceAsync(new()
                        {
                            DbHandle = dbHandle,
                            ResourceIndex = resourceIndex,
                            MaxLength = ushort.MaxValue,
                            Offset = 0
                        }, cancellationToken).ConfigureAwait(false);

                        database.Resources.Add(new()
                        {
                            ResourceId = resourceResponse.ResourceId,
                            Type = resourceResponse.Type,
                            Data = resourceResponse.Data
                        });
                    }
                    catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
                    {
                        break;
                    }
                }
            }

            private static async Task PullRecordsAsync(DlpConnection connection, byte dbHandle, RecordDatabase database, CancellationToken cancellationToken)
            {
                const int recordIdsPerRequest = 50;

                // Read record ids
                List<uint> recordIds = new();
                for (ushort startIndex = 0; ; startIndex = Convert.ToUInt16(recordIds.Count))
                {
                    try
                    {
                        ReadRecordIdListResponse recordIdListResponse = await connection.ReadRecordIdListAsync(new()
                        {
                            DbHandle = dbHandle,
                            Flags = 0,
                            MaxRecords = recordIdsPerRequest,
                            StartIndex = startIndex
                        }, cancellationToken).ConfigureAwait(false);

                        recordIds.AddRange(recordIdListResponse.RecordIds);

                        if (recordIdListResponse.Count < recordIdsPerRequest)
                        {
                            break;
                        }
                    }
                    catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
                    {
                        break;
                    }
                }

                // Read records
                foreach (uint recordId in recordIds)
                {
                    ReadRecordByIdResponse recordResponse = await connection.ReadRecordByIdAsync(new()
                    {
                        DbHandle = dbHandle,
                        RecordId = recordId,
                        MaxLength = ushort.MaxValue,
                        Offset = 0
                    }, cancellationToken).ConfigureAwait(false);

                    database.Records.Add(new()
                    {
                        UniqueId = recordResponse.RecordId,
                        Attributes = (DatabaseRecordAttributes)recordResponse.Attributes,
                        Category = recordResponse.Category,
                        Data = recordResponse.Data
                    });
                }
            }

            private static string GetFileName(Database database)
            {
                string safeName = Path.GetInvalidFileNameChars().Aggregate(database.Name, (current, c) => current.Replace(c, '_'));

                return Path.ChangeExtension(safeName, database.Attributes.HasFlag(DatabaseAttributes.ResourceDb) ? ".prc" : ".pdb");
            }
        }
    }
}