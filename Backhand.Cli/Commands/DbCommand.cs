using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
