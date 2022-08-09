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

            var databaseOption = new Option<string[]?>(
                name: "--database",
                description: "Name of database(s) to retrieve. If omitted, all databases will be retrieved.")
            {
                Arity = ArgumentArity.OneOrMore,
            };
            AddOption(databaseOption);

            this.SetHandler(DoPullDb, deviceOption, pathOption, databaseOption);
        }

        private async Task DoPullDb(string[] deviceNames, string path, string[]? databaseNames)
        {
            Func<DlpConnection, CancellationToken, Task> syncFunc = async (dlp, cancellationToken) =>
            {
                _logger.LogInformation("Beginning sync process.");

                _logger.LogInformation("Reading database list from device...");
                List<DlpDatabaseMetadata> metadataList = await ReadFullDbList(dlp, cancellationToken);
                _logger.LogInformation($"Found {metadataList.Count} databases.");

                if (databaseNames == null || databaseNames.Length == 0)
                {
                    databaseNames = metadataList.Select(md => md.Name).ToArray();
                }

                foreach (string databaseName in databaseNames)
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
                        await PullResourceDb(dlp, metadata, filePath, cancellationToken);
                    }
                }
            };

            _logger.LogInformation("Running device servers...");
            await RunDeviceServers(deviceNames, syncFunc);
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
                        }, cancellationToken);

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
            database.UniqueIdSeed = 0; // ?

            OpenDbResponse openDbResponse =
                await dlp.OpenDb(new OpenDbRequest
                {
                    CardId = 0,
                    Mode = OpenDbRequest.OpenDbMode.Read,
                    Name = metadata.Name
                }, cancellationToken);

            byte dbHandle = openDbResponse.DbHandle;

            const ushort readLengthPerRequest = 512;
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
                            MaxLength = readLengthPerRequest,
                        });

                    resourceId = readResourceResponse.Metadata.ResourceId;
                    resourceType = readResourceResponse.Metadata.Type;
                    resourceBuffer = new byte[readResourceResponse.Metadata.Size];
                    readResourceResponse.Data.CopyTo(resourceBuffer, 0);
                    offset += Convert.ToUInt16(readResourceResponse.Data.Length);
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
                            MaxLength = readLengthPerRequest,
                        });

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
            });

            // Write database to file
            using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 512, true))
            {
                await database.Serialize(fileStream);
            }
        }
    }
}
