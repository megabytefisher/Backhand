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

namespace Backhand.Cli.Commands.DbCommands
{
    public class PullCommand : SyncFuncCommand
    {
        private class SyncContext
        {
            public required string Name { get; init; }
            public required FileInfo? Output { get; init; }
            public required IConsole Console { get; init; }
        }

        private static readonly Option<string> NameOption = new(new[] { "--name", "-n" }, "The name of the database to pull.")
        {
            IsRequired = true
        };

        private static readonly Option<FileInfo> OutputOption = new(new[] { "--output", "-o" }, "The output file to write the database to.")
        {
        };

        public PullCommand()
            : base("pull", "Pull a database file from the device.")
        {
            Add(NameOption);
            Add(OutputOption);

            this.SetHandler(async (context) =>
            {
                string name = context.ParseResult.GetValueForOption(NameOption)!;
                FileInfo? output = context.ParseResult.GetValueForOption(OutputOption);

                IConsole console = context.Console;

                Func<DlpConnection, SyncContext> contextFactory = _ => new SyncContext
                {
                    Name = name,
                    Output = output,
                    Console = console
                };

                await RunDlpServerAsync<SyncContext>(context, SyncAsync, contextFactory).ConfigureAwait(false);
            });
        }

        private async Task SyncAsync(DlpConnection connection, SyncContext context, CancellationToken cancellationToken)
        {
            await connection.OpenConduitAsync().ConfigureAwait(false);

            List<DatabaseMetadata> metadataList = new();
            ushort startIndex = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ReadDbListResponse dbListResponse = await connection.ReadDbListAsync(new()
                {
                    Mode = ReadDbListMode.ListMultiple | ReadDbListMode.ListRam,
                    CardId = 0,
                    StartIndex = startIndex
                }, cancellationToken).ConfigureAwait(false);

                metadataList.AddRange(dbListResponse.Results);
                startIndex = (ushort)(dbListResponse.LastIndex + 1);

                if (dbListResponse.LastIndex == dbListResponse.Results.Last().Index)
                {
                    break;
                }
            }

            // Find our database
            DatabaseMetadata? dbMetadata = metadataList.FirstOrDefault(m => m.Name == context.Name);
            if (dbMetadata == null)
            {
                context.Console.WriteLine($"Database '{context.Name}' not found.");
                return;
            }

            bool isResource = dbMetadata.Attributes.HasFlag(DlpDatabaseAttributes.ResourceDb);
            Database db = isResource ? new ResourceDatabase() : new RecordDatabase();

            // Fill header info
            db.Name = dbMetadata.Name;
            db.Attributes = (DatabaseAttributes)dbMetadata.Attributes;
            db.Version = dbMetadata.Version;
            db.CreationDate = dbMetadata.CreationDate;
            db.ModificationDate = dbMetadata.ModificationDate;
            db.LastBackupDate = dbMetadata.LastBackupDate;
            db.ModificationNumber = dbMetadata.ModificationNumber;
            db.Type = dbMetadata.Type;
            db.Creator = dbMetadata.Creator;
            db.UniqueIdSeed = 0;

            // Open database on device
            OpenDbResponse openDbResponse = await connection.OpenDbAsync(new()
            {
                CardId = 0,
                Name = db.Name,
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

                db.AppInfo = appInfoResponse.Data;
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

                db.SortInfo = sortInfoResponse.Data;
            }
            catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
            {
                // No problem
            }

            // Fill entries
            if (isResource)
            {
                await PullResourcesAsync(connection, dbHandle, (ResourceDatabase)db, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await PullRecordsAsync(connection, dbHandle, (RecordDatabase)db, cancellationToken).ConfigureAwait(false);
            }

            // Close database on device
            await connection.CloseDbAsync(new()
            {
                DbHandle = dbHandle
            }, cancellationToken).ConfigureAwait(false);

            // Save database to file
            await using FileStream outStream = File.OpenWrite(context.Output?.FullName ?? GetFileName(db));
            await db.SerializeAsync(outStream, cancellationToken).ConfigureAwait(false);
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