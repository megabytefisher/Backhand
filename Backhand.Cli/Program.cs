using Backhand.Cli.Commands;
using Spectre.Console;
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
        public static readonly IAnsiConsole Console = AnsiConsole.Console;
        public static Parser Parser { get; private set; } = null!;

        public static async Task<int> Main(string[] args)
        {
            Parser = new CommandLineBuilder(new AppRootCommand())
                .UseDefaults()
                .AddMiddleware(AddServices)
                .Build();

            return await Parser.InvokeAsync(args);
        }

        private static Task AddServices(InvocationContext context, Func<InvocationContext, Task> next)
        {
            context.BindingContext.AddService(_ => Console);
            context.BindingContext.AddService(_ => Parser);

            return next(context);
        }
    }
}
