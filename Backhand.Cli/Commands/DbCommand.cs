using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace Backhand.Cli.Commands
{
    public class DbCommand : Command
    {
        public DbCommand(ILoggerFactory loggerFactory)
            : base("db", "Perform operations on a device's databases.")
        {
            AddCommand(new DbPullCommand(loggerFactory));
            AddCommand(new DbPushCommand(loggerFactory));
        }
    }
}
