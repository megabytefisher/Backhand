using Backhand.Cli.Internal.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Backhand.Cli.Internal.Commands
{
    public abstract class LoggableCommand : Command
    {
        private static readonly Option<bool> LogConsoleOption =
            new(new[] { "--log-console" }, "Log to console");

        protected LoggableCommand(string name, string description) : base(name, description)
        {
            Add(LogConsoleOption);
        }

        protected ILoggerFactory GetLoggerFactory(InvocationContext context)
        {
            ILoggerFactory? serviceLoggerFactory;
            if ((serviceLoggerFactory = context.BindingContext.GetService<ILoggerFactory>()) != null)
            {
                return serviceLoggerFactory;
            }

            AnsiConsoleLoggerProvider consoleLoggerProvider = context.BindingContext.GetRequiredService<AnsiConsoleLoggerProvider>();

            bool logConsole = context.ParseResult.GetValueForOption(LogConsoleOption);

            return LoggerFactory.Create(builder =>
            {
                ConfigureLogger(context, builder);

                if (logConsole)
                {
                    builder.AddProvider(consoleLoggerProvider);
                }
            });
        }

        protected virtual void ConfigureLogger(InvocationContext context, ILoggingBuilder loggerFactory)
        {
        }
    }
}