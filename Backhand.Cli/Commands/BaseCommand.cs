using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpServers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands
{
    public abstract class BaseCommand : Command
    {
        protected readonly ILoggerFactory _loggerFactory;
        protected readonly ILogger _logger;

        public BaseCommand(string name, string? description = null, ILoggerFactory? loggerFactory = null)
            : base(name, description)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger("Command");
        }

        protected async Task RunDeviceServers(string[] deviceNames, Func<DlpConnection, CancellationToken, Task> syncFunc, CancellationToken cancellationToken = default)
        {
            List<DlpServer> servers = new List<DlpServer>();

            foreach (string device in deviceNames)
            {
                if (device.ToLower() == "usb")
                {
                    servers.Add(new UsbDlpServer(syncFunc));
                }
                else
                {
                    servers.Add(new SerialDlpServer(device, syncFunc, _loggerFactory));
                }
            }

            List<Task> serverTasks = servers.Select(s => s.Run(cancellationToken)).ToList();

            await Task.WhenAll(serverTasks);
        }
    }
}
