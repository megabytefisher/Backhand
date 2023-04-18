using Backhand.Dlp;
using Backhand.Protocols.Dlp;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands
{
    public abstract class SyncFuncCommand : Command
    {
        protected readonly Option<string[]> DevicesOption = new(new[] { "--devices", "-d" })
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true
        };

        protected readonly Option<bool> DaemonOption = new(new[] { "--daemon", "-D" });

        public SyncFuncCommand(string name, string description) : base(name, description)
        {
            AddOption(DevicesOption);
            AddOption(DaemonOption);
        }

        protected async Task RunDlpServerAsync<TContext>(InvocationContext context, DlpSyncFunc<TContext> syncFunc, Func<DlpConnection, TContext>? contextFactory = null)
        {
            IConsole console = context.Console;
            CancellationToken cancellationToken = context.GetCancellationToken();
            string[] devicesString = context.ParseResult.GetValueForOption(DevicesOption)!;
            bool daemon = context.ParseResult.GetValueForOption(DaemonOption);

            if (!daemon && devicesString.Length > 1)
            {
                throw new ArgumentException("Cannot specify multiple devices without running in daemon mode.");
            }

            List<DlpServer<TContext>> servers = new List<DlpServer<TContext>>();
            foreach (string deviceString in devicesString)
            {
                string[] deviceParts = deviceString.Split(':');

                if (deviceParts[0] == "serial")
                {
                    servers.Add(new SerialDlpServer<TContext>(deviceParts[1], syncFunc, contextFactory));
                }
                else if (deviceParts[0] == "usb")
                {
                    servers.Add(new UsbDlpServer<TContext>(syncFunc, contextFactory));
                }
                else if (deviceParts[0] == "network")
                {
                    servers.Add(new NetworkDlpServer<TContext>(syncFunc, contextFactory));
                }
                else
                {
                    throw new ArgumentException($"Unknown device type: {deviceParts[0]}");
                }
            }

            EventHandler<DlpSyncEndedEventArgs<TContext>> OnSyncEnded = (sender, e) =>
            {
                if (e.SyncException != null)
                {
                    HandleSyncError(e.Connection, e.SyncException, console);
                }
            };

            IDlpServer<TContext> server = new AggregatedDlpServer<TContext>(servers);
            server.SyncEnded += OnSyncEnded;
            try
            {
                await server.RunAsync(!daemon, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                server.SyncEnded -= OnSyncEnded;
            }
        }

        protected virtual void HandleSyncError(DlpConnection connection, Exception ex, IConsole console)
        {
            console.WriteLine("Exception during sync:");
            console.WriteLine(ex.ToString());
        }
    }
}
