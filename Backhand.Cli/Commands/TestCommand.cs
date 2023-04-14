using Backhand.Cli.Internal;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands
{
    public class TestCommand : Command
    {
        private static readonly Option<string> InputOption = new(
            "--input",
            "Your input value")
        {
            IsRequired = true
        };

        public TestCommand() : base("test", "Test the thing")
        {
            AddOption(InputOption);

            this.SetHandler(RunCommandAsync, InputOption, Bind.FromServiceProvider<ILogger>());
        }

        private async Task RunCommandAsync(string input, ILogger logger)
        {
            logger.LogInformation($"Got input: {input}");
        }
    }
}
