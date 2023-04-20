using System.CommandLine;

namespace Backhand.Cli.Commands
{
    internal class AppRootCommand : RootCommand
    {
        public AppRootCommand() : base("Backhand CLI")
        {
            AddCommand(new PromptModeCommand() { IsHidden = true });
            AddCommand(new DeviceCommand());
            AddCommand(new DbCommand());
        }
    }
}
