using Backhand.DeviceIO.Cmp;
using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using Backhand.DeviceIO.DlpCommands.v1_0.Data;
using Backhand.DeviceIO.DlpServers;
using Backhand.DeviceIO.DlpTransports;
using Backhand.DeviceIO.Padp;
using Backhand.DeviceIO.Slp;
using Backhand.Pdb;
using System.Buffers;
using System.CommandLine;

namespace Backhand.Cli
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Backhand CLI Utility");

            // install command
            var installDeviceOption = new Option<string[]>(
                name: "--device",
                description: "Device(s) to use for communication. Either the name of a serial port or 'USB'.")
            {
                IsRequired = true,
                Arity = ArgumentArity.OneOrMore,
            };

            var installPathOption = new Option<string[]>(
                name: "--path",
                description: "Path to file(s) to install.")
            {
                IsRequired = true,
                Arity = ArgumentArity.OneOrMore,
            };

            var installCommand = new Command("install", "Install PDB/PRC files onto a device.")
            {
                installDeviceOption,
                installPathOption
            };
            rootCommand.AddCommand(installCommand);
            installCommand.SetHandler(DoInstall, installDeviceOption, installPathOption);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task DoInstall(string[] deviceNames, string[] paths)
        {
            Func<DlpConnection, CancellationToken, Task> syncFunc = async (dlp, cancellationToken) =>
            {
                foreach (string path in paths)
                {
                    switch (Path.GetExtension(path).ToLower())
                    {
                        case ".prc":
                            await InstallPrc(dlp, path);
                            break;
                            //case ".pdb":
                            //    break;
                    }
                }
            };

            await RunDeviceServers(deviceNames, syncFunc);
        }

        private static async Task RunDeviceServers(string[] deviceNames, Func<DlpConnection, CancellationToken, Task> syncFunc, CancellationToken cancellationToken = default)
        {
            List<DlpServer> servers = new List<DlpServer>();

            foreach (string device in deviceNames)
            {
                if (device.ToLower() == "usb")
                {
                    servers.Add(new UsbDlpServer(syncFunc));
                }
                else
                {
                    servers.Add(new SerialDlpServer(device, syncFunc));
                }
            }

            List<Task> serverTasks = servers.Select(s => s.Run(cancellationToken)).ToList();

            await Task.WhenAll(serverTasks);
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