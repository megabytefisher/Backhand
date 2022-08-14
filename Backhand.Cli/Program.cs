using Backhand.Cli.Commands;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Threading.Tasks;

namespace Backhand.Cli
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                }).AddFilter(l => true);
            });

            RootCommand rootCommand = new("Backhand CLI Utility")
            {
                new DbCommand(loggerFactory)
            };

            return await rootCommand.InvokeAsync(args);
        }
    }
}