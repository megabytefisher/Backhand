using Backhand.Cli.Commands;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace Backhand.Cli
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                }).AddFilter(l => true);
            });

            var rootCommand = new RootCommand("Backhand CLI Utility");

            rootCommand.Add(new DbCommand(loggerFactory));

            return await rootCommand.InvokeAsync(args);
        }
    }
}