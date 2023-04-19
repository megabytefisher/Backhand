using Backhand.Dlp;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.Logging;
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

        protected async Task RunDlpServerAsync<TContext>(InvocationContext context, ISyncHandler<TContext> syncHandler)
        {
            ILoggerFactory loggerFactory = GetLoggerFactory(context);
            IConsole console = context.Console;
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

                if (deviceParts[0] == "serial")
                {
                    console.WriteLine($"Listening on serial port {deviceParts[1]}");
                    serverTasks.Add(new SerialDlpServer<TContext>(deviceParts[1], loggerFactory).RunAsync(syncHandler, !daemon, cancellationToken));
                }
                else if (deviceParts[0] == "usb")
                {
                    console.WriteLine("Listening for USB devices");
                    serverTasks.Add(new UsbDlpServer<TContext>(loggerFactory).RunAsync(syncHandler, !daemon, cancellationToken));
                }
                else if (deviceParts[0] == "network")
                {
                    console.WriteLine("Listening for network devices");
                    serverTasks.Add(new NetworkDlpServer<TContext>(loggerFactory).RunAsync(syncHandler, !daemon, cancellationToken));
                }
                else
                {
                    throw new ArgumentException($"Unknown device type: {deviceParts[0]}");
                }
            }

            await Task.WhenAll(serverTasks).ConfigureAwait(false);
        }
    }
}
