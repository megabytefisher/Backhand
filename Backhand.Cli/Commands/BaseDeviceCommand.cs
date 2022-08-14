using Backhand.Cli.Exceptions;
using Backhand.DeviceIO.DlpServers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands
{
    public class BaseDeviceCommand : Command
    {
        protected Option<string[]> DeviceOption { get; }
        protected Option<bool> ServerOption { get; }

        protected ILoggerFactory LoggerFactory { get; }
        protected ILogger Logger { get; }

        protected BaseDeviceCommand(string name, string? description = null, ILoggerFactory? loggerFactory = null)
            : base(name, description)
        {
            LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            Logger = LoggerFactory.CreateLogger(GetType());

            DeviceOption = new Option<string[]>(
                name: "--device",
                description: "Device(s) to use for communication. Either the name of a serial port or 'USB'.")
            {
                IsRequired = false,
                Arity = ArgumentArity.OneOrMore,
            };
            AddOption(DeviceOption);

            ServerOption = new Option<bool>(
                name: "--server",
                description: "Operate in server mode. Multiple devices will be allowed to sync, and the program will not exit after the first sync.");
            AddOption(ServerOption);
        }

        protected async Task RunDeviceServers(string[] deviceNames, bool serverMode, Func<DlpContext, CancellationToken, Task> syncFunc, CancellationToken cancellationToken = default)
        {
            using SemaphoreSlim? syncSemaphore = serverMode ? null : new SemaphoreSlim(1);

            async Task SyncWrapperFunc(DlpContext ctx, CancellationToken ct)
            {
                if (syncSemaphore != null)
                {
                    if (!await syncSemaphore.WaitAsync(0, CancellationToken.None))
                    {
                        throw new SyncAlreadyStartedException();
                    }
                }

                await syncFunc(ctx, ct);
            }

            List<DlpServer> servers = new();
            foreach (string device in deviceNames)
            {
                if (device.ToLower() == "usb")
                {
                    servers.Add(new UsbDlpServer(SyncWrapperFunc));
                }
                else
                {
                    servers.Add(new SerialDlpServer(device, SyncWrapperFunc, LoggerFactory));
                }
            }

            TaskCompletionSource syncTcs = new();
            AggregatedDlpServer server = new(servers);

            server.SyncStarting += (s, e) =>
            {
                Logger.LogInformation("Device sync starting.");
            };

            server.SyncEnded += (s, e) =>
            {
                if (e.SyncException != null)
                {
                    Logger.LogError("Device sync failed.");
                }
                else
                {
                    Logger.LogInformation("Device sync completed.");
                }

                if (e.SyncException is not SyncAlreadyStartedException)
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
                using CancellationTokenSource abortCts = new();
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
                    // ignored
                }
            }
        }
    }
}
