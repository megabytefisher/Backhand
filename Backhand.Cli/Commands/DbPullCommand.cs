using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using Backhand.DeviceIO.DlpCommands.v1_0.Data;
using Backhand.DeviceIO.DlpServers;
using Backhand.Pdb;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands
{
    public class DbPullCommand : BaseDeviceCommand
    {
        public DbPullCommand(ILoggerFactory loggerFactory)
            : base("pull", "Downloads one or more databases from a connected device.", loggerFactory)
        {
            Option<string> pathOption = new(
                name: "--path",
                description: "Path to a directory in which to store the database files.")
            {
                IsRequired = true,
            };
            AddOption(pathOption);

            Option<string[]> databaseOption = new(
                name: "--database",
                description: "Name of database(s) to pull.")
            {
                Arity = ArgumentArity.OneOrMore,
            };
            AddOption(databaseOption);

            Option<bool> backupOption = new(
                name: "--backup",
                description: "Pulls all databases the device has marked as to be backed-up.");
            AddOption(backupOption);

            Option<bool> allOption = new(
                name: "--all",
                description: "Pulls all databases that exist on the device.");
            AddOption(allOption);

            this.SetHandler(RunCommandAsync, DeviceOption, ServerOption, pathOption, databaseOption, backupOption, allOption);
        }

        private async Task RunCommandAsync(string[] deviceNames, bool serverMode, string path, string[] databases, bool backup, bool all)
        {
            if (!Directory.Exists(path))
            {
                Logger.LogError($"Destination directory does not exist.");
                return;
            }

            async Task SyncFunc(DlpContext context, CancellationToken cancellationToken)
            {
                ReadUserInfoResponse userInfoResponse = await context.Connection.ReadUserInfoAsync(cancellationToken);

                string userPath;
                if (userInfoResponse.Username.Length > 0)
                {
                    string userDirName = $"{userInfoResponse.Username}-{userInfoResponse.UserId}";
                    userPath = Path.Combine(path, userDirName);
                    Logger.LogInformation($"Got user info. Saving databases under: {userDirName}");
                }
                else
                {
                    string userDirName = $"unknown-{DateTime.Now.ToFileTime()}";
                    userPath = Path.Combine(path, userDirName);
                    Logger.LogWarning($"No user info on device. Saving databases under: {userDirName}");
                }

                if (!Directory.Exists(userPath))
                {
                    Directory.CreateDirectory(userPath);
                }

                await context.Connection.OpenConduitAsync(cancellationToken);

                Logger.LogDebug($"Reading database list from device...");
                List<DlpDatabaseMetadata> metadataList = await ReadFullDbListAsync(context.Connection, cancellationToken).ConfigureAwait(false);
                Logger.LogDebug($"Found {metadataList.Count} databases.");

                IEnumerable<string> databaseNames = databases;
                if (all)
                {
                    databaseNames = metadataList.Select(md => md.Name);
                }
                else if (backup)
                {
                    databaseNames = databaseNames.Concat(metadataList.Where(md => md.Attributes.HasFlag(DlpDatabaseAttributes.Backup)).Select(md => md.Name));
                }

                databaseNames = databaseNames.Distinct().ToList();

                if (!databaseNames.Any())
                {
                    Logger.LogWarning($"Found no databases to pull from device.");
                }

                foreach (string databaseName in databaseNames)
                {
                    Logger.LogInformation($"Attempting to pull database: {databaseName}...");

                    DlpDatabaseMetadata? metadata = metadataList.FirstOrDefault(md => md.Name == databaseName);
                    if (metadata == null)
                    {
                        Logger.LogError($"Database does not exist on device: {databaseName}");
                        continue;
                    }

                    await PullDbAsync(context.Connection, metadata, userPath, cancellationToken).ConfigureAwait(false);
                }
            }

            Logger.LogInformation("Running device servers...");
            await RunDeviceServers(deviceNames, serverMode, SyncFunc).ConfigureAwait(false);
        }

        private async Task PullDbAsync(DlpConnection dlp, DlpDatabaseMetadata metadata, string outputPath, CancellationToken cancellationToken)
        {
            bool isResource = metadata.Attributes.HasFlag(DlpDatabaseAttributes.ResourceDb);
            Database database = isResource ?
                new ResourceDatabase() :
                new RecordDatabase();

            // Fill header info
            database.Name = metadata.Name;
            database.Attributes = (DatabaseAttributes)metadata.Attributes;
            database.Version = metadata.Version;
            database.CreationDate = metadata.CreationDate;
            database.ModificationDate = metadata.ModificationDate;
            database.LastBackupDate = metadata.LastBackupDate;
            database.ModificationNumber = metadata.ModificationNumber;
            database.Type = metadata.Type;
            database.Creator = metadata.Creator;
            database.UniqueIdSeed = 0;

            // Open database on device
            OpenDbResponse openDbResponse =
                await dlp.OpenDbAsync(new OpenDbRequest
                {
                    CardId = 0,
                    Mode = OpenDbRequest.OpenDbMode.Read,
                    Name = metadata.Name
                }, cancellationToken);

            byte dbHandle = openDbResponse.DbHandle;

            // Try reading AppInfo block from device database
            try
            {
                ReadAppBlockResponse readAppBlockResponse =
                    await dlp.ReadAppBlockAsync(new ReadAppBlockRequest
                    {
                        DbHandle = dbHandle,
                        Length = ushort.MaxValue,
                        Offset = 0
                    }, cancellationToken);

                database.AppInfo = readAppBlockResponse.Data;
            }
            catch (DlpCommandErrorException ex)
            {
                if (ex.ErrorCode != DlpErrorCode.NotFoundError)
                    throw;
            }

            // Try reading SortInfo block from device database
            try
            {
                ReadSortBlockResponse readSortBlockResponse =
                    await dlp.ReadSortBlockAsync(new ReadSortBlockRequest
                    {
                        DbHandle = dbHandle,
                        Length = ushort.MaxValue,
                        Offset = 0
                    }, cancellationToken);

                database.SortInfo = readSortBlockResponse.Data;
            }
            catch (DlpCommandErrorException ex)
            {
                if (ex.ErrorCode != DlpErrorCode.NotFoundError)
                    throw;
            }

            // Fill entries
            if (isResource)
            {
                await FillResourceDbAsync(dlp, dbHandle, (ResourceDatabase)database, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await FillRecordDbAsync(dlp, dbHandle, (RecordDatabase)database, cancellationToken).ConfigureAwait(false);
            }

            // Close device database
            await dlp.CloseDbAsync(new CloseDbRequest
            {
                DbHandle = dbHandle
            }, cancellationToken);

            // Write our in-memory database to file
            string databaseFileName = Path.ChangeExtension(GetSafeFileName(metadata.Name), isResource ? "PRC" : "PDB");
            string databaseFilePath = Path.Combine(outputPath, databaseFileName);
            
            await using FileStream outStream = new(
                databaseFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                true);
            await database.SerializeAsync(outStream);
        }

        private static async Task<List<DlpDatabaseMetadata>> ReadFullDbListAsync(DlpConnection dlp, CancellationToken cancellationToken)
        {
            List<DlpDatabaseMetadata> metadataList = new();
            int index = 0;
            while (true)
            {
                try
                {
                    ReadDbListResponse readDbListResponse =
                        await dlp.ReadDbListAsync(new ReadDbListRequest
                        {
                            CardId = 0,
                            Mode = ReadDbListRequest.ReadDbListMode.ListRam | ReadDbListRequest.ReadDbListMode.ListMultiple,
                            StartIndex = Convert.ToUInt16(index),
                        }, cancellationToken).ConfigureAwait(false);

                    metadataList.AddRange(readDbListResponse.Metadata);
                    index = readDbListResponse.LastIndex + 1;
                }
                catch (DlpCommandErrorException ex)
                {
                    if (ex.ErrorCode == DlpErrorCode.NotFoundError)
                    {
                        // End of entries
                        break;
                    }
                }
            }

            return metadataList;
        }

        private static async Task FillResourceDbAsync(DlpConnection dlp, byte dbHandle, ResourceDatabase database, CancellationToken cancellationToken)
        {
            for (ushort resourceIndex = 0; ; resourceIndex++)
            {
                try
                {
                    ReadResourceByIndexResponse readResourceResponse =
                        await dlp.ReadResourceByIndexAsync(new ReadResourceByIndexRequest
                        {
                            DbHandle = dbHandle,
                            ResourceIndex = resourceIndex,
                            MaxLength = ushort.MaxValue,
                            Offset = 0
                        }, cancellationToken);

                    if (readResourceResponse.Data.Length != readResourceResponse.Metadata.Size)
                    {
                        throw new Exception("Didn't read whole resource");
                    }

                    database.Resources.Add(new DatabaseResource
                    {
                        ResourceId = readResourceResponse.Metadata.ResourceId,
                        Type = readResourceResponse.Metadata.Type,
                        Data = readResourceResponse.Data
                    });
                }
                catch (DlpCommandErrorException ex)
                {
                    if (ex.ErrorCode == DlpErrorCode.NotFoundError)
                        break;
                }
            }
        }

        private static async Task FillRecordDbAsync(DlpConnection dlp, byte dbHandle, RecordDatabase database, CancellationToken cancellationToken)
        {
            // Read record IDs
            List<uint> recordIds = new();
            for (ushort startIndex = 0; ; startIndex = Convert.ToUInt16(recordIds.Count))
            {
                try
                {
                    ReadRecordIdListResponse recordIdListResponse =
                        await dlp.ReadRecordIdListAsync(new ReadRecordIdListRequest
                        {
                            DbHandle = dbHandle,
                            Flags = 0,
                            MaxRecords = 50,
                            StartIndex = startIndex
                        }, cancellationToken).ConfigureAwait(false);

                    recordIds.AddRange(recordIdListResponse.RecordIds);
                }
                catch (DlpCommandErrorException ex)
                {
                    if (ex.ErrorCode == DlpErrorCode.NotFoundError)
                        break;
                }
            }

            // Read records..
            foreach (uint recordId in recordIds)
            {
                ReadRecordByIdResponse readRecordResponse =
                    await dlp.ReadRecordByIdAsync(new ReadRecordByIdRequest
                    {
                        DbHandle = dbHandle,
                        RecordId = recordId,
                        MaxLength = ushort.MaxValue,
                        Offset = 0
                    }, cancellationToken).ConfigureAwait(false);

                if (readRecordResponse.Data.Length != readRecordResponse.Metadata.Length)
                {
                    throw new Exception("Didn't read whole resource");
                }

                database.Records.Add(new DatabaseRecord
                {
                    UniqueId = readRecordResponse.Metadata.RecordId,
                    Attributes = (RecordAttributes)readRecordResponse.Metadata.Attributes,
                    Data = readRecordResponse.Data
                });
            }
        }

        private static string GetSafeFileName(string name)
        {
            return Path.GetInvalidFileNameChars()
                .Aggregate(name, (current, c) => current.Replace(c, '_'));
        }
    }
}
