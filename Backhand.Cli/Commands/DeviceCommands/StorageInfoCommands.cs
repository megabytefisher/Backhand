using System.CommandLine;
using Backhand.Cli.Commands.DeviceCommands.StorageInfoCommands;

namespace Backhand.Cli.Commands.DeviceCommands
{
    public class StorageInfoCommand : Command
    {
        public StorageInfoCommand() : base("storageinfo", "Contains commands for manipulating storage info on a connected device")
        {
            AddCommand(new ReadCommand());
        }
    }
}
