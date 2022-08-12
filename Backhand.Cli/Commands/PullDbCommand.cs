using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using Backhand.DeviceIO.DlpCommands.v1_0.Data;
using Backhand.Pdb;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands
{
    public class PullDbCommand : BaseCommand
    {
        public PullDbCommand(ILoggerFactory loggerFactory)
            : base("pulldb", "Downloads one or more databases from a connected device.", loggerFactory)
        {
            var deviceOption = new Option<string[]>(
                name: "--device",
                description: "Device(s) to use for communication. Either the name of a serial port or 'USB'.")
            {
                IsRequired = true,
                Arity = ArgumentArity.OneOrMore,
            };
            AddOption(deviceOption);

            var pathOption = new Option<string>(
                name: "--path",
                description: "Path to a directory in which to store the database files.")
            {
                IsRequired = true,
            };
            AddOption(pathOption);

            var databaseOption = new Option<string[]>(
                name: "--database",
                description: "Name of database(s) to retrieve. If omitted, all databases will be retrieved.")
            {
                Arity = ArgumentArity.OneOrMore,
            };
            AddOption(databaseOption);

            this.SetHandler(DoPullDb, deviceOption, pathOption, databaseOption);
        }

        private async Task DoPullDb(string[] deviceNames, string path, string[] databaseNames)
        {
            Func<DlpConnection, CancellationToken, Task> syncFunc = async (dlp, cancellationToken) =>
            {
                _logger.LogInformation("Beginning sync process.");

                await dlp.OpenConduit();

                _logger.LogDebug("Reading database list from device...");
                List<DlpDatabaseMetadata> metadataList = await ReadFullDbList(dlp, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug($"Found {metadataList.Count} databases.");

                foreach (string databaseName in (databaseNames != null && databaseNames.Length > 0 ?
                                                    databaseNames :
                                                    metadataList.Where(md => md.Attributes.HasFlag(DlpDatabaseAttributes.Backup)).Select(md => md.Name)))
                {
                    _logger.LogInformation($"Attempting to pull database: {databaseName}...");

                    DlpDatabaseMetadata? metadata = metadataList.FirstOrDefault(md => md.Name == databaseName);
                    if (metadata == null)
                    {
                        _logger.LogError($"Database does not exist on device: {databaseName}");
                        continue;
                    }

                    if (metadata.Attributes.HasFlag(DlpDatabaseAttributes.ResourceDb))
                    {
                        string filePath = Path.ChangeExtension(Path.Combine(path, GetSafeFileName(databaseName)), ".PRC");
                        await PullResourceDb(dlp, metadata, filePath, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        string filePath = Path.ChangeExtension(Path.Combine(path, GetSafeFileName(databaseName)), ".PDB");
                        await PullRecordDb(dlp, metadata, filePath, cancellationToken).ConfigureAwait(false);
                    }
                }

                _logger.LogInformation("Completed sync process.");
            };

            _logger.LogInformation("Running device servers...");
            await RunDeviceServers(deviceNames, syncFunc).ConfigureAwait(false);
        }

        private string GetSafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        private async Task<List<DlpDatabaseMetadata>> ReadFullDbList(DlpConnection dlp, CancellationToken cancellationToken)
        {
            List<DlpDatabaseMetadata> metadataList = new List<DlpDatabaseMetadata>();
            int index = 0;
            while (true)
            {
                try
                {
                    ReadDbListResponse readDbListResponse =
                        await dlp.ReadDbList(new ReadDbListRequest
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

        private async Task PullResourceDb(DlpConnection dlp, DlpDatabaseMetadata metadata, string outputPath, CancellationToken cancellationToken)
        {
            ResourceDatabase database = new ResourceDatabase();
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

            OpenDbResponse openDbResponse =
                await dlp.OpenDb(new OpenDbRequest
                {
                    CardId = 0,
                    Mode = OpenDbRequest.OpenDbMode.Read,
                    Name = metadata.Name
                }, cancellationToken).ConfigureAwait(false);

            byte dbHandle = openDbResponse.DbHandle;

            try
            {
                ReadAppBlockResponse readAppBlockResponse =
                    await dlp.ReadAppBlock(new ReadAppBlockRequest
                    {
                        DbHandle = dbHandle,
                        Length = ushort.MaxValue,
                        Offset = 0
                    });

                database.AppInfo = readAppBlockResponse.Data;
            }
            catch (DlpCommandErrorException ex)
            {
                if (ex.ErrorCode != DlpErrorCode.NotFoundError)
                    throw;
            }

            try
            {
                ReadSortBlockResponse readSortBlockResponse =
                    await dlp.ReadSortBlock(new ReadSortBlockRequest
                    {
                        DbHandle = dbHandle,
                        Length = ushort.MaxValue,
                        Offset = 0
                    });

                database.SortInfo = readSortBlockResponse.Data;
            }
            catch (DlpCommandErrorException ex)
            {
                if (ex.ErrorCode != DlpErrorCode.NotFoundError)
                    throw;
            }

            for (ushort resourceIndex = 0; true; resourceIndex++)
            {
                ushort resourceId;
                string? resourceType;
                ushort offset = 0;
                byte[]? resourceBuffer;
                ushort resourceLength = 0;

                // Can we read ANY of it?
                try
                {
                    ReadResourceByIndexResponse readResourceResponse =
                        await dlp.ReadResourceByIndex(new ReadResourceByIndexRequest
                        {
                            DbHandle = dbHandle,
                            ResourceIndex = resourceIndex,
                            Offset = 0,
                            MaxLength = 0,
                        }).ConfigureAwait(false);

                    resourceId = readResourceResponse.Metadata.ResourceId;
                    resourceType = readResourceResponse.Metadata.Type;
                    resourceBuffer = new byte[readResourceResponse.Metadata.Size];
                    resourceLength = readResourceResponse.Metadata.Size;
                    
                }
                catch (DlpCommandErrorException ex)
                {
                    if (ex.ErrorCode == DlpErrorCode.NotFoundError)
                    {
                        // No more resources.
                        break;
                    }

                    throw;
                }

                while (offset < resourceLength)
                {
                    ReadResourceByIndexResponse readResourceResponse =
                        await dlp.ReadResourceByIndex(new ReadResourceByIndexRequest
                        {
                            DbHandle = dbHandle,
                            ResourceIndex = resourceIndex,
                            Offset = offset,
                            MaxLength = resourceLength,
                        }).ConfigureAwait(false);

                    readResourceResponse.Data.CopyTo(((Span<byte>)resourceBuffer).Slice(offset));
                    offset += Convert.ToUInt16(readResourceResponse.Data.Length);
                }

                // Add record to in-memory database
                database.Resources.Add(new DatabaseResource
                {
                    ResourceId = resourceId,
                    Type = resourceType,
                    Data = resourceBuffer
                });
            }

            await dlp.CloseDb(new CloseDbRequest
            {
                DbHandle = dbHandle,
            }).ConfigureAwait(false);

            // Write database to file
            using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 512, true))
            {
                await database.Serialize(fileStream).ConfigureAwait(false);
            }
        }

        private async Task PullRecordDb(DlpConnection dlp, DlpDatabaseMetadata metadata, string outputPath, CancellationToken cancellationToken)
        {
            RecordDatabase database = new RecordDatabase();
            database.Name = metadata.Name;
            database.Attributes = (DatabaseAttributes)metadata.Attributes;
            database.Version = metadata.Version;
            database.CreationDate = metadata.CreationDate;
            database.ModificationDate = metadata.ModificationDate;
            database.LastBackupDate = metadata.LastBackupDate;
            database.ModificationNumber = metadata.ModificationNumber;
            database.Type = metadata.Type;
            database.Creator = metadata.Creator;
            database.UniqueIdSeed = 0; // ?

            OpenDbResponse openDbResponse =
                await dlp.OpenDb(new OpenDbRequest
                {
                    CardId = 0,
                    Mode = OpenDbRequest.OpenDbMode.Read,
                    Name = metadata.Name
                }, cancellationToken).ConfigureAwait(false);

            byte dbHandle = openDbResponse.DbHandle;

            try
            {
                ReadAppBlockResponse readAppBlockResponse =
                    await dlp.ReadAppBlock(new ReadAppBlockRequest
                    {
                        DbHandle = dbHandle,
                        Length = ushort.MaxValue,
                        Offset = 0
                    });

                database.AppInfo = readAppBlockResponse.Data;
            }
            catch (DlpCommandErrorException ex)
            {
                if (ex.ErrorCode != DlpErrorCode.NotFoundError)
                    throw;
            }

            try
            {
                ReadSortBlockResponse readSortBlockResponse =
                    await dlp.ReadSortBlock(new ReadSortBlockRequest
                    {
                        DbHandle = dbHandle,
                        Length = ushort.MaxValue,
                        Offset = 0
                    });

                database.SortInfo = readSortBlockResponse.Data;
            }
            catch (DlpCommandErrorException ex)
            {
                if (ex.ErrorCode != DlpErrorCode.NotFoundError)
                    throw;
            }

            // Read record ids
            List<uint> recordIds = new List<uint>();
            for (ushort startIndex = 0; true; startIndex = Convert.ToUInt16(recordIds.Count))
            {
                try
                {
                    ReadRecordIdListResponse recordIdListResponse =
                        await dlp.ReadRecordIdList(new ReadRecordIdListRequest
                        {
                            DbHandle = dbHandle,
                            Flags = 0,
                            StartIndex = startIndex,
                            MaxRecords = 50
                        }).ConfigureAwait(false);

                    recordIds.AddRange(recordIdListResponse.RecordIds);
                }
                catch (DlpCommandErrorException ex)
                {
                    if (ex.ErrorCode == DlpErrorCode.NotFoundError)
                    {
                        break;
                    }
                }
            }

            // Read records..
            foreach (uint recordId in recordIds)
            {
                //uint recordId = 0x381005;
                ushort recordOffset = 0;
                ushort? recordLength = null;
                byte[]? recordBuffer = null;
                DlpRecordMetadata? recordMetadata = null;

                do
                {
                    ReadRecordByIdResponse readRecordResponse =
                        await dlp.ReadRecordById(new ReadRecordByIdRequest
                        {
                            DbHandle = dbHandle,
                            RecordId = recordId,
                            MaxLength = 1024,
                            Offset = recordOffset
                        }).ConfigureAwait(false);

                    if (recordBuffer == null)
                    {
                        recordMetadata = readRecordResponse.Metadata;
                        recordLength = readRecordResponse.Metadata.Length;
                        recordBuffer = new byte[recordLength.Value];
                    }

                    readRecordResponse.Data.CopyTo(((Span<byte>)recordBuffer).Slice(recordOffset));
                    recordOffset += Convert.ToUInt16(readRecordResponse.Data.Length);
                } while (recordOffset < recordLength!.Value);

                DatabaseRecord record = new DatabaseRecord
                {
                    UniqueId = recordId,
                    Attributes = (RecordAttributes)recordMetadata!.Attributes,
                    Data = recordBuffer
                };
                database.Records.Add(record);
            }

            await dlp.CloseDb(new CloseDbRequest
            {
                DbHandle = dbHandle,
            }).ConfigureAwait(false);

            // Write database to file
            using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 512, true))
            {
                await database.Serialize(fileStream).ConfigureAwait(false);
            }
        }
    }
}
