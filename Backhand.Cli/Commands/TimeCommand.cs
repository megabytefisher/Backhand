using System.CommandLine;
using Backhand.Cli.Commands.TimeCommands;

namespace Backhand.Cli.Commands
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
