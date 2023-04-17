using Backhand.Cli.Commands.DbCommands;
using System.CommandLine;

namespace Backhand.Cli.Commands
{
    public class DbCommand : Command
    {
        public DbCommand() : base("db", "Contains commands for manipulating databases on a connected device")
        {
            AddCommand(new ListCommand());
            AddCommand(new PushCommand());
            AddCommand(new PullCommand());
        }
    }
}
