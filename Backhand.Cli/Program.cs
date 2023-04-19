using Backhand.Cli.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Backhand.Cli
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            /*ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });*/

            Parser parser = new CommandLineBuilder(new AppRootCommand())
                .UseDefaults()
                //.AddMiddleware((context, next) => InjectServices(context, next, loggerFactory))
                .Build();

            return await parser.InvokeAsync(args);
        }

        /*private static async Task InjectServices(InvocationContext context, Func<InvocationContext, Task> next, ILoggerFactory loggerFactory)
        {
            Type commandType = context.BindingContext.ParseResult.CommandResult.Command.GetType();
            context.BindingContext.AddService((_) => loggerFactory);
            context.BindingContext.AddService((serviceProvider) => BuildLoggerService(serviceProvider, loggerFactory, commandType));

            await next(context);
        }*/

        private static ILogger BuildLoggerService(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, Type commandType)
        {
            return loggerFactory.CreateLogger(commandType);
        }
    }
}
