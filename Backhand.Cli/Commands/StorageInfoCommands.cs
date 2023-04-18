using System.CommandLine;
using Backhand.Cli.Commands.StorageInfoCommands;

namespace Backhand.Cli.Commands
{
    public class StorageInfoCommand : Command
    {
        public StorageInfoCommand() : base("storageinfo", "Contains commands for manipulating storage info on a connected device")
        {
            AddCommand(new ReadCommand());
        }
    }
}
