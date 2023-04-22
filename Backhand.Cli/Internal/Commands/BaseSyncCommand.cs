using Backhand.Dlp;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Cli.Internal.Commands
{
    public abstract class BaseSyncCommand : LoggableCommand
    {
        protected static readonly Option<string[]> ServersOption =
            new(new[] { "--servers", "-s" })
            {
                IsRequired = true,
                AllowMultipleArgumentsPerToken = true
            };

        protected static readonly Option<bool> DaemonOption =
            new(new[] { "--daemon", "-D" });

        private static readonly Option<LogLevel> SlpVerbosityOption =
            new(new[] { "--log-slp" }, () => LogLevel.Information, "SLP logging verbosity");

        private static readonly Option<LogLevel> PadpVerbosityOption =
            new(new[] { "--log-padp" }, () => LogLevel.Information, "PADP logging verbosity");

        private static readonly Option<LogLevel> CmpVerbosityOption =
            new(new[] { "--log-cmp" }, () => LogLevel.Information, "CMP logging verbosity");

        private static readonly Option<LogLevel> NetSyncVerbosityOption =
            new(new[] { "--log-netsync" }, () => LogLevel.Information, "NetSync logging verbosity");

        private static readonly Option<LogLevel> DlpVerbosityOption =
            new(new[] { "--log-dlp" }, () => LogLevel.Information, "DLP logging verbosity");

        protected BaseSyncCommand(string name, string description) : base(name, description)
        {
            Add(ServersOption);
            Add(DaemonOption);
            Add(SlpVerbosityOption);
            Add(PadpVerbosityOption);
            Add(CmpVerbosityOption);
            Add(NetSyncVerbosityOption);
            Add(DlpVerbosityOption);
        }

        protected override void ConfigureLogger(InvocationContext context, ILoggingBuilder builder)
        {
            LogLevel slpVerbosity = context.ParseResult.GetValueForOption(SlpVerbosityOption);
            LogLevel padpVerbosity = context.ParseResult.GetValueForOption(PadpVerbosityOption);
            LogLevel cmpVerbosity = context.ParseResult.GetValueForOption(CmpVerbosityOption);
            LogLevel netSyncVerbosity = context.ParseResult.GetValueForOption(NetSyncVerbosityOption);
            LogLevel dlpVerbosity = context.ParseResult.GetValueForOption(DlpVerbosityOption);

            builder.AddFilter(DlpServerLogging.SlpInterfaceCategory, slpVerbosity);
            builder.AddFilter(DlpServerLogging.PadpConnectionCategory, padpVerbosity);
            builder.AddFilter(DlpServerLogging.CmpConnectionCategory, cmpVerbosity);
            builder.AddFilter(DlpServerLogging.NetSyncInterfaceCategory, netSyncVerbosity);
            builder.AddFilter(DlpServerLogging.NetSyncConnectionCategory, netSyncVerbosity);
            builder.AddFilter(DlpServerLogging.DlpConnectionCategory, dlpVerbosity);
            builder.AddFilter(DlpServerLogging.DlpServerCategory, dlpVerbosity);
        }

        public abstract Task<ICommandSyncHandler> GetSyncHandlerAsync(InvocationContext context, CancellationToken cancellationToken);

        protected async Task RunDlpServerAsync<TContext>(InvocationContext context, CommandSyncHandler<TContext> syncHandler) where TContext : CommandSyncContext
        {
            IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();
            ILoggerFactory loggerFactory = GetLoggerFactory(context);

            string[] serversString = context.ParseResult.GetValueForOption(ServersOption)!;
            bool daemon = context.ParseResult.GetValueForOption(DaemonOption);

            CancellationToken cancellationToken = context.GetCancellationToken();

            if (!daemon && serversString.Length > 1)
            {
                throw new ArgumentException("Multiple devices can only be used in daemon mode.");
            }

            List<IDlpServer> servers = new();
            foreach (string deviceString in serversString)
            {
                string[] deviceParts = deviceString.Split(':');

                servers.Add(deviceParts[0] switch
                {
                    "serial" => new SerialDlpServer(deviceParts[1], loggerFactory),
                    "usb" => new UsbDlpServer(loggerFactory),
                    "network" => new NetworkDlpServer(loggerFactory),
                    _ => throw new ArgumentException($"Unknown device type: {deviceParts[0]}")
                });
            }
            
            Task[] serverTasks = servers.Select(
                server => RunServerAsync(server, syncHandler, !daemon, console, cancellationToken)
            ).ToArray();

            try
            {
                await Task.WhenAll(serverTasks).ConfigureAwait(false);
                console.MarkupLine("[green]Operation completed successfully.[/]");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                console.MarkupLine("[gray]Operation cancelled.[/]");
                context.ExitCode = ExitCodes.Aborted;
            }
            catch (Exception ex)
            {
                console.MarkupLine("[bold red]Operation failed.[/]");
                console.WriteException(ex);
                context.ExitCode = ExitCodes.Error;
            }
        }

        private static async Task RunServerAsync(IDlpServer server, ISyncHandler syncHandler, bool singleSync, IAnsiConsole console, CancellationToken cancellationToken)
        {
            try
            {
                console.MarkupLineInterpolated($"[gray]Sync server starting: {server.ToString()}[/]");
                await server.RunAsync(syncHandler, singleSync, cancellationToken).ConfigureAwait(false);
                console.MarkupLineInterpolated($"[gray]Sync server stopped: {server.ToString()}[/]");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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

        protected class CommandSyncContext
        {
            public required DlpConnection Connection { get; init; }
            public required IAnsiConsole Console { get; init; }
        }

        public interface ICommandSyncHandler : ISyncHandler
        {
            bool PrintDefaultMessages { get; set; }
        }

        protected abstract class CommandSyncHandler<T> : SyncHandler<T>, ICommandSyncHandler where T : CommandSyncContext
        {
            public required IAnsiConsole Console { get; init; }
            public bool PrintDefaultMessages { get; set; } = true;

            protected abstract Task<T> BuildContextInternalAsync(DlpConnection connection, CancellationToken cancellationToken);

            public override Task<T> InitializeAsync(DlpConnection connection, CancellationToken cancellationToken)
            {
                return BuildContextInternalAsync(connection, cancellationToken);
            }

            public override Task OnSyncStartedAsync(T context, CancellationToken cancellationToken)
            {
                if (PrintDefaultMessages)
                {
                    context.Console.MarkupLineInterpolated($"[bold green]Sync started: {context.Connection.ToString()}[/]");
                }

                return Task.CompletedTask;
            }

            public override Task OnSyncEndedAsync(T context, Exception? exception, CancellationToken cancellationToken)
            {
                if (PrintDefaultMessages)
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
                }

                return Task.CompletedTask;
            }
        }

        protected abstract class CommandSyncHandler : CommandSyncHandler<CommandSyncContext>
        {
            protected override Task<CommandSyncContext> BuildContextInternalAsync(DlpConnection connection, CancellationToken cancellationToken)
            {
                return Task.FromResult(new CommandSyncContext
                {
                    Connection = connection,
                    Console = Console
                });
            }
        }

        protected class AggregatedSyncHandler : CommandSyncHandler
        {
            private readonly ISyncHandler[] _syncHandlers;

            public AggregatedSyncHandler(IEnumerable<ISyncHandler> syncHandlers)
            {
                _syncHandlers = syncHandlers.ToArray();
            }

            public override async Task OnSyncAsync(CommandSyncContext context, CancellationToken cancellationToken)
            {
                foreach (ISyncHandler syncHandler in _syncHandlers)
                {
                    await syncHandler.OnSyncAsync(context.Connection, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
