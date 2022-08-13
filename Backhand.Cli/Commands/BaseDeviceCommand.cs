using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpServers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backhand.Cli.Exceptions;

namespace Backhand.Cli.Commands
{
    public class BaseDeviceCommand : Command
    {
        protected readonly Option<string[]> _deviceOption;
        protected readonly Option<bool> _serverOption;

        protected readonly ILoggerFactory _loggerFactory;
        protected readonly ILogger _logger;

        public BaseDeviceCommand(string name, string? description = null, ILoggerFactory? loggerFactory = null)
            : base(name, description)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger(GetType());

            _deviceOption = new Option<string[]>(
                name: "--device",
                description: "Device(s) to use for communication. Either the name of a serial port or 'USB'.")
            {
                IsRequired = false,
                Arity = ArgumentArity.OneOrMore,
            };
            AddOption(_deviceOption);

            _serverOption = new Option<bool>(
                name: "--server",
                description: "Operate in server mode. Multiple devices will be allowed to sync, and the program will not exit after the first sync.");
            AddOption(_serverOption);
        }

        protected async Task RunDeviceServers(string[] deviceNames, bool serverMode, Func<DlpContext, CancellationToken, Task> syncFunc, CancellationToken cancellationToken = default)
        {
            using SemaphoreSlim? syncSemaphore = serverMode ? null : new SemaphoreSlim(1);

            Func<DlpContext, CancellationToken, Task> syncWrapperFunc = async (ctx, ct) =>
            {
                if (syncSemaphore != null)
                {
                    if (!syncSemaphore.Wait(0))
                    {
                        throw new SyncAlreadyStartedException();
                    }
                }

                await syncFunc(ctx, ct);
            };

            List<DlpServer> servers = new List<DlpServer>();
            foreach (string device in deviceNames)
            {
                if (device.ToLower() == "usb")
                {
                    servers.Add(new UsbDlpServer(syncWrapperFunc));
                }
                else
                {
                    servers.Add(new SerialDlpServer(device, syncWrapperFunc, _loggerFactory));
                }
            }

            TaskCompletionSource syncTcs = new TaskCompletionSource();
            AggregatedDlpServer server = new AggregatedDlpServer(servers);

            server.SyncStarting += (s, e) =>
            {
                _logger.LogInformation("Device sync starting.");
            };

            server.SyncEnded += (s, e) =>
            {
                if (e.SyncException != null)
                {
                    _logger.LogError("Device sync failed.");
                }
                else
                {
                    _logger.LogInformation("Device sync completed.");
                }

                if (!(e.SyncException is SyncAlreadyStartedException))
                {
                    syncTcs.TrySetResult();
                }
            };

            if (serverMode)
            {
                await server.Run(cancellationToken);
            }
            else
            {
                using CancellationTokenSource abortCts = new CancellationTokenSource();
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, abortCts.Token);

                Task serverTask = server.Run(linkedCts.Token);

                await syncTcs.Task;

                abortCts.Cancel();

                try
                {
                    await serverTask;
                }
                catch
                {
                }
            }
        }
    }
}
