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
    public class InstallCommand : BaseCommand
    {
        public InstallCommand(ILoggerFactory loggerFactory)
            : base("install", "Installs a PDB/PRC file onto a connected device.", loggerFactory)
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
                description: "Path to file(s) to install.")
            {
                IsRequired = true,
                Arity = ArgumentArity.OneOrMore,
            };
            AddOption(pathOption);

            this.SetHandler(DoInstall, deviceOption, pathOption);
        }

        private async Task DoInstall(string[] deviceNames, string[] paths)
        {
            List<string> filePaths = GetFilePaths(paths);
            _logger.LogInformation($"Will install {filePaths.Count} file(s) to device.");

            Func<DlpConnection, CancellationToken, Task> syncFunc = async (dlp, cancellationToken) =>
            {
                _logger.LogInformation("Beginning sync process");

                foreach (string filePath in filePaths)
                {
                    _logger.LogInformation($"Installing: {filePath}");
                    switch (Path.GetExtension(filePath).ToLower())
                    {
                        case ".prc":
                            await InstallPrc(dlp, filePath);
                            break;
                            //case ".pdb":
                            //    break;
                    }
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

        private static async Task InstallPrc(DlpConnection dlp, string path)
        {
            ResourceDatabase database = new ResourceDatabase();

            using (FileStream inStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 512, true))
            {
                await database.Deserialize(inStream);
            }

            // Create database
            CreateDbResponse createDbResponse =
                await dlp.CreateDb(new CreateDbRequest
                {
                    Creator = database.Creator,
                    Type = database.Type,
                    CardId = 0,
                    Attributes = (DlpDatabaseAttributes)database.Attributes,
                    Version = database.Version,
                    Name = database.Name,
                });

            byte dbHandle = createDbResponse.DbHandle;

            // Write each resource
            foreach (DatabaseResource resource in database.Resources)
            {
                await dlp.WriteResource(new WriteResourceRequest
                {
                    DbHandle = dbHandle,
                    Type = resource.Type,
                    ResourceId = resource.ResourceId,
                    Size = Convert.ToUInt16(resource.Data.Length),
                    Data = resource.Data,
                });
            }

            // Close database
            await dlp.CloseDb(new CloseDbRequest
            {
                DbHandle = dbHandle
            });
        }
    }
}
