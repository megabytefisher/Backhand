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

        public SyncFuncCommand(string name, string description) : base(name, description)
        {
            AddOption(DevicesOption);
        }

        protected IDlpServer GetDlpServer(InvocationContext context, DlpSyncFunc syncFunc)
        {
            List<DlpServer> servers = new List<DlpServer>();
            string[] devicesString = context.ParseResult.GetValueForOption(DevicesOption)!;
            
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
                else
                {
                    throw new ArgumentException($"Unknown device type: {deviceParts[0]}");
                }
            }

            return new AggregatedDlpServer(servers);
        }
    }
}
