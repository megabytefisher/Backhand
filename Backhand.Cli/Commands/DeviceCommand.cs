using System.CommandLine;
using Backhand.Cli.Commands.DeviceCommands;

namespace Backhand.Cli.Commands
{
    internal class DeviceCommand : Command
    {
        public DeviceCommand() : base("device", "Commands for manipulating connected devices")
        {
            AddCommand(new SysInfoCommand());
            AddCommand(new UserInfoCommand());
            AddCommand(new DeviceCommands.DbCommand());
            AddCommand(new StorageInfoCommand());
            AddCommand(new TimeCommand());
        }
    }
}
