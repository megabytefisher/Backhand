using Backhand.Cli.Commands;
using Backhand.DeviceIO.Cmp;
using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using Backhand.DeviceIO.DlpCommands.v1_0.Data;
using Backhand.DeviceIO.DlpServers;
using Backhand.DeviceIO.DlpTransports;
using Backhand.DeviceIO.Padp;
using Backhand.DeviceIO.Slp;
using Backhand.Pdb;
using Microsoft.Extensions.Logging;
using System.Buffers;
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
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                });
            });

            var rootCommand = new RootCommand("Backhand CLI Utility");
            rootCommand.AddCommand(new InstallCommand(loggerFactory));
            rootCommand.AddCommand(new PullDbCommand(loggerFactory));
            return await rootCommand.InvokeAsync(args);
        }
    }
}