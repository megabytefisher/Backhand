using System.CommandLine;

namespace Backhand.Cli.Commands
{
    public class AppRootCommand : RootCommand
    {
        public AppRootCommand() : base("Backhand CLI")
        {
            AddCommand(new DeviceCommand());
            AddCommand(new DbCommand());
            AddCommand(new PromptModeCommand());
        }
    }
}
