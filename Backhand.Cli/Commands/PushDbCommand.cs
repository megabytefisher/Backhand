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
    public class PushDbCommand : BaseCommand
    {
        public PushDbCommand(ILoggerFactory loggerFactory)
            : base("pushdb", "Uploads one or more databases (in .PRC/.PDB file format) to a connected device.", loggerFactory)
        {
            var deviceOption = new Option<string[]>(
                name: "--device",
                description: "Device(s) to use for communication. Either the name of a serial port or 'USB'.")
            {
                IsRequired = true,
                Arity = ArgumentArity.OneOrMore,
            };
            AddOption(deviceOption);

            var pathOption = new Option<string[]>(
                name: "--path",
                description: "Path to database file(s) to install. If a directory is specified, database files will be recursively pushed.")
            {
                IsRequired = true,
                Arity = ArgumentArity.OneOrMore,
            };
            AddOption(pathOption);

            var overwriteOption = new Option<bool>(
                name: "--overwrite",
                description: "If a database already exists on the device, it will be deleted and rewritten.");
            AddOption(overwriteOption);

            this.SetHandler(DoPushDb, deviceOption, pathOption, overwriteOption);
        }

        private async Task DoPushDb(string[] deviceNames, string[] paths, bool overwrite)
        {
            List<string> filePaths = GetFilePaths(paths);
            _logger.LogInformation($"Will install {filePaths.Count} file(s) to device.");

            Func<DlpConnection, CancellationToken, Task> syncFunc = async (dlp, cancellationToken) =>
            {
                _logger.LogInformation("Beginning sync process");

                await dlp.OpenConduitAsync();

                foreach (string filePath in filePaths)
                {
                    _logger.LogInformation($"Installing: {filePath}");
                    await InstallDbAsync(dlp, filePath, overwrite, cancellationToken);
                }

                _logger.LogInformation("Sync complete");
            };

            _logger.LogInformation($"Running device servers...");
            await RunDeviceServers(deviceNames, syncFunc);
        }

        private static List<string> GetFilePaths(string[] paths)
        {
            List<string> results = new List<string>();
            foreach (string path in paths)
            {
                FillFilePaths(results, path);
            }
            return results;
        }

        private static void FillFilePaths(List<string> filePaths, string path)
        {
            bool isDirectory = File.GetAttributes(path).HasFlag(FileAttributes.Directory);

            if (isDirectory)
            {
                foreach (string innerPath in Directory.GetDirectories(path).Concat(Directory.GetFiles(path)))
                {
                    FillFilePaths(filePaths, innerPath);
                }
            }
            else
            {
                string fileExtension = Path.GetExtension(path).ToLower();

                if (fileExtension == ".prc" || fileExtension == ".pdb")
                {
                    filePaths.Add(path);
                }
            }
        }

        private async Task InstallDbAsync(DlpConnection dlp, string path, bool overwrite, CancellationToken cancellationToken)
        {
            bool isResource = Path.GetExtension(path).ToLower() == ".prc";
            Database database = isResource ?
                new ResourceDatabase() :
                new RecordDatabase();

            using (FileStream inStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                await database.DeserializeAsync(inStream);
            }

            CreateDbRequest createDbRequest = new CreateDbRequest
            {
                Creator = database.Creator,
                Type = database.Type,
                CardId = 0,
                Attributes = (DlpDatabaseAttributes)database.Attributes,
                Version = database.Version,
                Name = database.Name,
            };

            CreateDbResponse createDbResponse;
            try
            {
                createDbResponse = await dlp.CreateDbAsync(createDbRequest, cancellationToken);
            }
            catch (DlpCommandErrorException ex)
            {
                if (ex.ErrorCode == DlpErrorCode.AlreadyExistsError)
                {
                    if (overwrite)
                    {
                        _logger.LogInformation($"Database '{database.Name}' already exists on device. Deleting and rewriting...");

                        // Delete existing
                        await dlp.DeleteDbAsync(new DeleteDbRequest
                        {
                            CardId = 0,
                            Name = database.Name
                        }, cancellationToken);

                        // Try creating again..
                        createDbResponse = await dlp.CreateDbAsync(createDbRequest, cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning($"Database '{database.Name}' already exists on device. Skipping.");
                        return;
                    }
                }
                else
                {
                    throw;
                }
            }

            byte dbHandle = createDbResponse.DbHandle;

            // Write AppInfo block
            if (database.AppInfo != null && database.AppInfo.Length > 0)
            {
                await dlp.WriteAppBlockAsync(new WriteAppBlockRequest
                {
                    DbHandle = dbHandle,
                    Data = database.AppInfo,
                }, cancellationToken);
            }

            // Write SortInfo block
            if (database.SortInfo != null && database.SortInfo.Length > 0)
            {
                await dlp.WriteSortBlockAsync(new WriteSortBlockRequest
                {
                    DbHandle = dbHandle,
                    Data = database.SortInfo,
                }, cancellationToken);
            }

            // Install entries
            if (isResource)
            {
                await InstallResourcesAsync(dlp, dbHandle, (ResourceDatabase)database, cancellationToken);
            }
            else
            {
                await InstallRecordsAsync(dlp, dbHandle, (RecordDatabase)database, cancellationToken);
            }

            // Close device database
            await dlp.CloseDbAsync(new CloseDbRequest
            {
                DbHandle = dbHandle
            }, cancellationToken);
        }

        private static async Task InstallResourcesAsync(DlpConnection dlp, byte dbHandle, ResourceDatabase database, CancellationToken cancellationToken)
        {
            foreach (DatabaseResource resource in database.Resources)
            {
                await dlp.WriteResourceAsync(new WriteResourceRequest
                {
                    DbHandle = dbHandle,
                    Type = resource.Type,
                    ResourceId = resource.ResourceId,
                    Size = Convert.ToUInt16(resource.Data.Length),
                    Data = resource.Data
                });
            }
        }

        private static async Task InstallRecordsAsync(DlpConnection dlp, byte dbHandle, RecordDatabase database, CancellationToken cancellationToken)
        {
            foreach (DatabaseRecord record in database.Records)
            {
                await dlp.WriteRecordAsync(new WriteRecordRequest
                {
                    DbHandle = dbHandle,
                    RecordId = record.UniqueId,
                    Attributes = (DlpRecordAttributes)record.Attributes,
                    Category = record.Category,
                    Data = record.Data
                });
            }
        }
    }
}
