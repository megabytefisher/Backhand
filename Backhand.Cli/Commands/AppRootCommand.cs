using System.CommandLine;

namespace Backhand.Cli.Commands
{
    internal class AppRootCommand : RootCommand
    {
        public AppRootCommand() : base("Backhand CLI")
        {
            AddCommand(new DbCommand());
            AddCommand(new UserInfoCommand());
            AddCommand(new SysInfoCommand());
            AddCommand(new TimeCommand());
            AddCommand(new StorageInfoCommand());
        }
    }
}
