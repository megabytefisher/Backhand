using Backhand.Cli.Internal.Commands;
using Backhand.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands.DeviceCommands
{
    public class PromptModeCommand : BaseSyncCommand
    {
        public PromptModeCommand()
            : base("prompt-mode", "Executes commands on a device in a prompt mode")
        {
            this.SetHandler(async (context) =>
            {
                using PromptModeSyncHandler syncHandler = await GetSyncHandlerInternalAsync(context).ConfigureAwait(false);
                await RunDlpServerAsync(context, syncHandler).ConfigureAwait(false);
            });
        }

        public override async Task<ICommandSyncHandler> GetSyncHandlerAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            return await GetSyncHandlerInternalAsync(context);
        }

        private Task<PromptModeSyncHandler> GetSyncHandlerInternalAsync(InvocationContext context)
        {
            IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();
            ILoggerFactory loggerFactory = GetLoggerFactory(context);

            PromptModeSyncHandler syncHandler = new()
            {
                Console = console,
                LoggerFactory = loggerFactory
            };

            return Task.FromResult(syncHandler);
        }

        private sealed class PromptModeSyncHandler : CommandSyncHandler, IDisposable
        {
            public required ILoggerFactory LoggerFactory { get; init; }

            private readonly SemaphoreSlim _syncSemaphore = new(1);

            public void Dispose()
            {
                _syncSemaphore.Dispose();
            }

            public override async Task OnSyncAsync(CommandSyncContext context, CancellationToken cancellationToken)
            {
                if (!await _syncSemaphore.WaitAsync(0, cancellationToken))
                {
                    // Don't allow multiple syncs at once in prompt mode
                    throw new InvalidOperationException("Cannot sync multiple devices at once while in prompt mode");
                }
                
                using DisposableCallback semaphoreLock = new(() => _syncSemaphore.Release());

                context.Console.MarkupLineInterpolated($"[green]Connected to {context.Client.ToString()} in prompt mode. Enter device commands to execute or 'end' to end the session.[/]");

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    string input = await new TextPrompt<string>("> device").ShowAsync(context.Console, cancellationToken);

                    if (input == "end")
                    {
                        break;
                    }

                    DeviceCommand deviceCommand = new(true);
                    ParseResult parseResult = deviceCommand.Parse(input);
                    InvocationContext subInvocationContext = new(parseResult);

                    if (parseResult.CommandResult.Command is not BaseSyncCommand syncCommand)
                    {
                        if (parseResult.Errors.Count > 0)
                        {
                            foreach (ParseError error in parseResult.Errors)
                            {
                                context.Console.MarkupLine($"[bold red]{error.Message}[/]");
                            }

                            StringWriter helpWriter = new();
                            subInvocationContext.HelpBuilder.Write(parseResult.CommandResult.Command, helpWriter);
                            context.Console.MarkupInterpolated($"{helpWriter.ToString()}");
                            continue;
                        }

                        context.Console.MarkupLine("[bold red]Command was not a device sync command.[/]");
                        continue;
                    }

                    subInvocationContext.BindingContext.AddService(_ => LoggerFactory);
                    await Program.AddServicesAsync(subInvocationContext.BindingContext);
                    ICommandSyncHandler syncHandler = await syncCommand.GetSyncHandlerAsync(subInvocationContext, cancellationToken);
                    syncHandler.PrintDefaultMessages = false;

                    try
                    {
                        await syncHandler.OnSyncAsync(context.Client, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        context.Console.MarkupLine($"[red]Command failed with exception.[/]");
                        context.Console.WriteException(ex);
                        continue;
                    }

                    context.Console.MarkupLine(subInvocationContext.ExitCode == 0
                        ? $"[gray]Command completed successfully.[/]"
                        : $"[red]Command failed with exit code {subInvocationContext.ExitCode}.[/]");
                }
            }
        }
    }
}
