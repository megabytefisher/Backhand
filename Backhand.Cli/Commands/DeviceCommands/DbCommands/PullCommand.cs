using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Pdb;
using Backhand.Protocols.Dlp;
using OpenDbMode = Backhand.Dlp.Commands.v1_0.Arguments.OpenDbRequest.OpenDbMode;
using DatabaseMetadata = Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListResponse.DatabaseMetadata;
using ReadDbListMode = Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListRequest.ReadDbListMode;
using Backhand.Dlp;
using Backhand.Cli.Internal;

namespace Backhand.Cli.Commands.DeviceCommands.DbCommands
{
    public class PullCommand : BaseSyncCommand
    {
        private static readonly Option<string[]> NameOption =
            new(new[] { "--name", "-n" }, "The name of the database to pull.")
            {
                Arity = ArgumentArity.ZeroOrMore
            };

        private static readonly Option<FileInfo> OutputOption =
            new(new[] { "--output", "-o" }, "The output file to write the database to.");
            
        private readonly Option<IEnumerable<ReadDbListMode>> ReadModesOption =
            new(new[] { "--read-modes", "-m" }, () => new[] { ReadDbListMode.ListMultiple | ReadDbListMode.ListRam })
            {
                AllowMultipleArgumentsPerToken = true
            };

        private readonly Option<IEnumerable<DlpDatabaseAttributes>> AttributesOption =
            new(new[] { "--attributes", "-a" }, () => new[] { DlpDatabaseAttributes.Backup }, "Only pulls databases with the specified attribute(s).");

        public PullCommand()
            : base("pull", "Pull a database file from the device.")
        {
            Add(NameOption);
            Add(OutputOption);
            Add(ReadModesOption);
            Add(AttributesOption);

            this.SetHandler(async (context) =>
            {
                string[] names = context.ParseResult.GetValueForOption(NameOption)!;
                FileInfo? output = context.ParseResult.GetValueForOption(OutputOption);
                ReadDbListMode readMode = context.ParseResult.GetValueForOption(ReadModesOption)!.Aggregate(ReadDbListMode.None, (acc, cur) => acc | cur);
                DlpDatabaseAttributes attributes = context.ParseResult.GetValueForOption(AttributesOption)!.Aggregate(DlpDatabaseAttributes.None, (acc, cur) => acc | cur);

                IConsole console = context.Console;

                PullSyncHandler syncHandler = new()
                {
                    Names = names,
                    Output = output,
                    ReadMode = readMode,
                    Attributes = attributes,
                    Console = console
                };

                await RunDlpServerAsync<PullSyncContext>(context, syncHandler).ConfigureAwait(false);
            });
        }

        private class PullSyncContext
        {
            public required DlpConnection Connection { get; init; }
            public required string[] Names { get; init; }
            public required FileInfo? Output { get; init; }
            public required ReadDbListMode ReadMode { get; init; }
            public required DlpDatabaseAttributes Attributes { get; init; }
            public required IConsole Console { get; init; }
        }

        private class PullSyncHandler : ISyncHandler<PullSyncContext>
        {
            public required string[] Names { get; init; }
            public required FileInfo? Output { get; init; }
            public required ReadDbListMode ReadMode { get; init; }
            public required DlpDatabaseAttributes Attributes { get; init; }
            public required IConsole Console { get; init; }

            public Task<PullSyncContext> InitializeAsync(DlpConnection connection, CancellationToken cancellationToken)
            {
                return Task.FromResult(new PullSyncContext
                {
                    Connection = connection,
                    Names = Names,
                    ReadMode = ReadMode,
                    Attributes = Attributes,
                    Output = Output,
                    Console = Console
                });
            }

            public async Task OnSyncAsync(PullSyncContext context, CancellationToken cancellationToken)
            {
                await context.Connection.OpenConduitAsync(cancellationToken).ConfigureAwait(false);

                List<DatabaseMetadata> dbResults = new List<DatabaseMetadata>();
                ushort startIndex = 0;
                while (true)
                {
                    try
                    {
                        ReadDbListResponse response = await context.Connection.ReadDbListAsync(new ReadDbListRequest
                        {
                            Mode = ReadMode,
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

                IEnumerable<DatabaseMetadata> pullDatabases =
                    Names.Length > 0 ? dbResults.Where(db => Names.Contains(db.Name, StringComparer.OrdinalIgnoreCase)) :
                    Attributes != DlpDatabaseAttributes.None ? dbResults.Where(db => db.Attributes.HasFlag(Attributes)) :
                    dbResults;

                foreach (DatabaseMetadata dbMetadata in pullDatabases)
                {
                    Database database = await PullDatabaseAsync(context.Connection, dbMetadata, cancellationToken).ConfigureAwait(false);

                    // Save database to file
                    await using FileStream outStream = File.OpenWrite(context.Output?.FullName ?? GetFileName(database));
                    await database.SerializeAsync(outStream, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task<Database> PullDatabaseAsync(DlpConnection connection, DatabaseMetadata databaseMetadata, CancellationToken cancellationToken)
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

                        database.Resources.Add(new() {
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

                    database.Records.Add(new() {
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