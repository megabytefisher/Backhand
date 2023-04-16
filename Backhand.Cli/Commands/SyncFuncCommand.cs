using Backhand.Dlp;
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
        protected IDlpServer DlpServer { get; private set; } = null!;

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

        protected async Task RunDlpServerAsync(InvocationContext context, DlpSyncFunc syncFunc)
        {
            CancellationToken cancellationToken = context.GetCancellationToken();
            string[] devicesString = context.ParseResult.GetValueForOption(DevicesOption)!;
            bool daemon = context.ParseResult.GetValueForOption(DaemonOption);

            if (!daemon && devicesString.Length > 1)
            {
                throw new ArgumentException("Cannot specify multiple devices without running in daemon mode.");
            }

            DlpSyncFunc innerSyncFunc = (dlpConnection, cancellationToken) =>
            {
                if (daemon)
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        syncFunc(dlpConnection, cancellationToken);
                    }
                    return Task.CompletedTask;
                }
                else
                {
                    return syncFunc(dlpConnection, cancellationToken);
                }
            };

            List<DlpServer> servers = new List<DlpServer>();
            foreach (string deviceString in devicesString)
            {
                string[] deviceParts = deviceString.Split(':');

                if (deviceParts[0] == "serial")
                {
                    servers.Add(new SerialDlpServer(syncFunc, deviceParts[1]));
                }
                else if (deviceParts[0] == "usb")
                {
                    servers.Add(new UsbDlpServer(syncFunc));
                }
                else if (deviceParts[0] == "network")
                {
                    servers.Add(new NetworkDlpServer(syncFunc));
                }
                else
                {
                    throw new ArgumentException($"Unknown device type: {deviceParts[0]}");
                }
            }

            IDlpServer server = new AggregatedDlpServer(servers);

            await server.RunAsync(!daemon, cancellationToken).ConfigureAwait(false);
        }
    }
}
