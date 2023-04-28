using System.CommandLine;
using Backhand.Cli.Commands.DbCommands;

namespace Backhand.Cli.Commands
{
    internal class DbCommand : Command
    {
        public DbCommand() : base("db", "Commands for manipulating database files")
        {
            //AddCommand(new ConvertCommand());
            AddCommand(new DisassembleCommand());
            //AddCommand(new AssembleCommand());
        }
    }
}
