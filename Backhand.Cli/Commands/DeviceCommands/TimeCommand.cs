using System.CommandLine;
using Backhand.Cli.Commands.DeviceCommands.TimeCommands;

namespace Backhand.Cli.Commands.DeviceCommands
{
    public class TimeCommand : Command
    {
        public TimeCommand() : base("time", "Contains commands for manipulating the system time and date on a connected device")
        {
            AddCommand(new ReadCommand());
            AddCommand(new WriteCommand());
        }
    }
}
