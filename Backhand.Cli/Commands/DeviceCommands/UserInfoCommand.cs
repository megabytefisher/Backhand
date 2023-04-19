using System.CommandLine;
using Backhand.Cli.Commands.DeviceCommands.UserInfoCommands;

namespace Backhand.Cli.Commands
{
    public class UserInfoCommand : Command
    {
        public UserInfoCommand() : base("userinfo", "Contains commands for manipulating user info on a connected device")
        {
            AddCommand(new ReadCommand());
            AddCommand(new WriteCommand());
        }
    }
}
