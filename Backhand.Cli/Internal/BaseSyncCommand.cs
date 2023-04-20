using Backhand.Dlp;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Cli.Internal
{
    public abstract class BaseSyncCommand : LoggableCommand
    {
        protected readonly Option<string[]> DevicesOption = new(new[] { "--devices", "-d" })
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true
        };

        protected readonly Option<bool> DaemonOption = new(new[] { "--daemon", "-D" });

        public BaseSyncCommand(string name, string description) : base(name, description)
        {
            AddOption(DevicesOption);
            AddOption(DaemonOption);
        }

        protected async Task RunDlpServerAsync<TContext>(InvocationContext context, ISyncHandler<TContext> syncHandler)// where TContext : SyncContext
        {
            IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();
            ILoggerFactory loggerFactory = GetLoggerFactory(context);
            CancellationToken cancellationToken = context.GetCancellationToken();
            string[] devicesString = context.ParseResult.GetValueForOption(DevicesOption)!;
            bool daemon = context.ParseResult.GetValueForOption(DaemonOption);

            if (!daemon && devicesString.Length > 1)
            {
                throw new ArgumentException("Cannot specify multiple devices without running in daemon mode.");
            }

            List<Task> serverTasks = new();
            foreach (string deviceString in devicesString)
            {
                string[] deviceParts = deviceString.Split(':');

                DlpServer<TContext> server;
                if (deviceParts[0] == "serial")
                {
                    SerialDlpServer<TContext> serialServer;
                    server = serialServer = new SerialDlpServer<TContext>(deviceParts[1], loggerFactory);
                    Task runTask = serialServer.RunAsync(syncHandler, !daemon, cancellationToken);
                    serverTasks.Add(WrapServerRunTaskAsync(server, runTask, console, cancellationToken));
                }
                else if (deviceParts[0] == "usb")
                {
                    UsbDlpServer<TContext> usbServer;
                    server = usbServer = new UsbDlpServer<TContext>(loggerFactory);
                    Task runTask = usbServer.RunAsync(syncHandler, !daemon, cancellationToken);
                    serverTasks.Add(WrapServerRunTaskAsync(server, runTask, console, cancellationToken));
                }
                else if (deviceParts[0] == "network")
                {
                    NetworkDlpServer<TContext> networkServer;
                    server = networkServer = new NetworkDlpServer<TContext>(loggerFactory);
                    Task runTask = networkServer.RunAsync(syncHandler, !daemon, cancellationToken);
                    serverTasks.Add(WrapServerRunTaskAsync(server, runTask, console, cancellationToken));
                }
                else
                {
                    throw new ArgumentException($"Unknown device type: {deviceParts[0]}");
                }

                console.MarkupLineInterpolated($"[green]Sync server started: {server.ToString()}[/]");
            }

            try
            {
                await Task.WhenAll(serverTasks).ConfigureAwait(false);
                console.MarkupLine("[green]Operation completed successfully.[/]");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                console.MarkupLine("[gray]Operation cancelled.[/]");
                context.ExitCode = 1;
            }
            catch (Exception ex)
            {
                console.WriteException(ex);
                context.ExitCode = 1;
            }
        }

        private async Task WrapServerRunTaskAsync<TContext>(DlpServer<TContext> server, Task runTask, IAnsiConsole console, CancellationToken userRequestedCancellationToken)
        {
            try
            {
                await runTask;
                console.MarkupLineInterpolated($"[green]Sync server stopped: {server.ToString()}[/]");
            }
            catch (OperationCanceledException) when (userRequestedCancellationToken.IsCancellationRequested)
            {
                console.MarkupLineInterpolated($"[gray]Sync server stopped due cancellation request: {server.ToString()}[/]");
                throw;
            }
            catch (Exception ex)
            {
                console.MarkupLineInterpolated($"[red]Sync server stopped due to error: {server.ToString()}[/]");
                console.WriteException(ex);
                throw;
            }
        }

        protected class SyncContext
        {
            public required DlpConnection Connection { get; init; }
            public required IAnsiConsole Console { get; init; }
        }

        protected abstract class SyncHandler<T> : ISyncHandler<T> where T : SyncContext
        {
            public required IAnsiConsole Console { get; init; }

            protected abstract Task<T> GetContextAsync(DlpConnection connection, CancellationToken cancellationToken);

            public Task<T> InitializeAsync(DlpConnection connection, CancellationToken cancellationToken)
            {
                return GetContextAsync(connection, cancellationToken);
            }

            public Task OnSyncStartedAsync(T context, CancellationToken cancellationToken)
            {
                context.Console.MarkupLineInterpolated($"[bold green]Sync started: {context.Connection.ToString()}[/]");

                return Task.CompletedTask;
            }

            public abstract Task OnSyncAsync(T context, CancellationToken cancellationToken);

            public Task OnSyncEndedAsync(T context, Exception? exception, CancellationToken cancellationToken)
            {
                if (exception != null)
                {
                    context.Console.MarkupLineInterpolated($"[bold red]Sync ended with error: {context.Connection.ToString()}[/]");
                    context.Console.WriteException(exception);
                }
                else
                {
                    context.Console.MarkupLineInterpolated($"[bold green]Sync ended: {context.Connection.ToString()}[/]");
                }

                return Task.CompletedTask;
            }
        }
    }
}
