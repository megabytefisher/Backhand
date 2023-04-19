using Backhand.Protocols.Slp;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Backhand.Cli.Internal
{
    public abstract class LoggableCommand : Command
    {
        public LoggableCommand(string name, string description) : base(name, description)
        {
        }

        protected ILoggerFactory GetLoggerFactory(InvocationContext context)
        {
            return LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                /*builder.AddFilter((category, level) => 
                {
                    if (category == typeof(SlpInterface).FullName)
                    {
                        return false;
                    }

                    return level >= LogLevel.Debug;
                });*/
            });
        }
    }
}