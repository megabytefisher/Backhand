using Backhand.DeviceIO.Cmp;
using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.Dlp.Arguments;
using Backhand.DeviceIO.DlpTransports;
using Backhand.DeviceIO.Padp;
using Backhand.DeviceIO.Slp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServer
{
    public class SerialDlpServer : DlpServer
    {
        private string _portName;

        public SerialDlpServer(string portName)
        {
            _portName = portName;
        }

        public override async Task Run(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Task? syncTask = null;

                using CancellationTokenSource abortCts = new CancellationTokenSource();
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(abortCts.Token, cancellationToken);

                try
                {
                    using SlpDevice slpDevice = new SlpDevice(_portName);
                    using PadpConnection padpConnection = new PadpConnection(slpDevice, 3, 3, 0xff);

                    CmpConnection cmpConnection = new CmpConnection(padpConnection);

                    Task waitForWakeUpTask = cmpConnection.WaitForWakeUpAsync(linkedCts.Token);
                    Task ioTask = slpDevice.RunIOAsync(linkedCts.Token);

                    await waitForWakeUpTask;
                    await cmpConnection.DoHandshakeAsync();

                    using PadpDlpTransport dlpTransport = new PadpDlpTransport(padpConnection);
                    DlpConnection dlpConnection = new DlpConnection(dlpTransport);

                    // Do sync
                    syncTask = DoSync(dlpConnection, linkedCts.Token);
                    try
                    {
                        await Task.WhenAny(syncTask, ioTask);
                    }
                    catch
                    {
                    }

                    abortCts.Cancel();

                    await Task.WhenAll(syncTask, ioTask);
                }
                catch (TaskCanceledException ex)
                {
                    if (syncTask != null)
                    {
                        if (syncTask.IsCompletedSuccessfully)
                        {
                            // Swallow
                        }
                        else
                        {
                            throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
