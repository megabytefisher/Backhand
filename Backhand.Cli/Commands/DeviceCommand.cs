using System.CommandLine;
using Backhand.Cli.Commands.DeviceCommands;

namespace Backhand.Cli.Commands
{
    internal class DeviceCommand : Command
    {
        public const string CommandName = "device";

        public DeviceCommand(bool interactive = false) : base(CommandName, "Commands for manipulating connected devices")
        {
            AddCommand(new SysInfoCommand());
            AddCommand(new UserInfoCommand());
            AddCommand(new DeviceCommands.DbCommand());
            AddCommand(new StorageInfoCommand());
            AddCommand(new TimeCommand());

            if (!interactive)
            {
                AddCommand(new DeviceCommands.PromptModeCommand());
            }
        }
    }
}
