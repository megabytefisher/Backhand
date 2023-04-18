using System.CommandLine;
using Backhand.Cli.Commands.SysInfoCommands;

namespace Backhand.Cli.Commands
{
    public class SysInfoCommand : Command
    {
        public SysInfoCommand() : base("sysinfo", "Contains commands for manipulating system info on a connected device")
        {
            AddCommand(new ReadCommand());
        }
    }
}
