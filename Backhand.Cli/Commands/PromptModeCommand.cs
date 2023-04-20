using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands
{
    public class PromptModeCommand : Command
    {
        public PromptModeCommand() : base("prompt-mode", "Executes commands in a prompt mode")
        {
            this.SetHandler(async (context) =>
            {
                IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();
                Parser parser = context.BindingContext.GetRequiredService<Parser>();

                CancellationToken cancellationToken = context.GetCancellationToken();

                while (true)
                {
                    string input = await new TextPrompt<string>("Enter command:").ShowAsync(console, cancellationToken);
                    int exitCode = await parser.InvokeAsync(input).ConfigureAwait(false);

                    if (exitCode == 0)
                    {
                        console.MarkupLine($"[gray]Command completed successfully.[/]");
                    }
                    else
                    {
                        console.MarkupLine($"[red]Command failed with exit code {exitCode}.[/]");
                    }
                }
            });
        }
    }
}
