using Backhand.Cli.Commands;
using Backhand.Cli.Internal.Logging;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Backhand.Cli
{
    public static class Program
    {
        public static readonly AppRootCommand RootCommand = new();
        public static readonly IAnsiConsole Console = AnsiConsole.Console;
        internal static readonly AnsiConsoleLoggerProvider ConsoleLoggerProvider = new(Console);
        public static readonly Parser RootParser = new CommandLineBuilder(RootCommand)
            .UseDefaults()
            .AddMiddleware(AddServicesAsync)
            .Build();

        public static async Task<int> Main(string[] args)
        {
            return await RootParser.InvokeAsync(args);
        }

        public static async Task AddServicesAsync(InvocationContext context, Func<InvocationContext, Task> next)
        {
            await AddServicesAsync(context.BindingContext);
            await next(context);
        }

        public static Task AddServicesAsync(BindingContext context)
        {
            context.AddService(_ => (RootCommand)RootCommand);
            context.AddService(_ => Console);
            context.AddService(_ => ConsoleLoggerProvider);
            context.AddService(_ => RootParser);
            return Task.CompletedTask;
        }
    }
}
