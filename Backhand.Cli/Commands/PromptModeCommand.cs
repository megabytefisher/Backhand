using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading;

namespace Backhand.Cli.Commands
{
    public class PromptModeCommand : Command
    {
        public PromptModeCommand()
            : base("prompt-mode", "Executes commands in a prompt mode")
        {
            this.SetHandler(async (context) =>
            {
                IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();
                Parser parser = context.BindingContext.GetRequiredService<Parser>();

                CancellationToken cancellationToken = context.GetCancellationToken();

                console.MarkupLineInterpolated($"[gray]Running in prompt mode. Enter commands to execute or 'end' to end the session.[/]");

                while (true)
                {
                    string input = await new TextPrompt<string>(">").ShowAsync(console, cancellationToken);

                    if (input == "end")
                    {
                        break;
                    }

                    int exitCode = await parser.InvokeAsync(input).ConfigureAwait(false);

                    switch (exitCode)
                    {
                        case ExitCodes.Success:
                            console.MarkupLine($"[gray]Command completed successfully.[/]");
                            break;
                        case ExitCodes.Aborted:
                            console.MarkupLine($"[gray]Command aborted.[/]");
                            break;
                        default:
                            console.MarkupLine($"[red]Command failed with exit code {exitCode}.[/]");
                            break;
                    }
                }
            });
        }
    }
}
