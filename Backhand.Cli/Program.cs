using Backhand.DeviceIO.Cmp;
using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.Dlp.Arguments;
using Backhand.DeviceIO.DlpServer;
using Backhand.DeviceIO.DlpTransports;
using Backhand.DeviceIO.Padp;
using Backhand.DeviceIO.Slp;
using Backhand.DeviceIO.Usb.Windows;
using MadWizard.WinUSBNet;
using System.Buffers;

namespace Backhand.Cli
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //SerialDlpServer server = new SerialDlpServer("COM3");
            UsbDlpServer server = new UsbDlpServer();
            await server.Run();

            /*USBDeviceInfo deviceInfo = USBDevice.GetDevices("dee824ef-729b-4a0e-9c14-b7117d33a817").FirstOrDefault();
            WindowsUsbNetSyncDevice netSyncDevice = new WindowsUsbNetSyncDevice(deviceInfo);

            Task ioTask = netSyncDevice.RunIOAsync();
            await netSyncDevice.DoNetSyncHandshake();

            NetSyncDlpTransport transport = new NetSyncDlpTransport(netSyncDevice);*/

            /*SlpDevice slpDevice = new SlpDevice("COM3");
            Task ioTask = slpDevice.RunIOAsync();

            using PadpConnection padp = new PadpConnection(slpDevice, 3, 3, 0xff);

            CmpConnection cmp = new CmpConnection(padp);
            await cmp.DoHandshakeAsync();

            using PadpDlpTransport transport = new PadpDlpTransport(padp);*/

            /*transport.SendingPayload += (s, e) =>
            {
                Console.WriteLine($"SEND DLP");
                Console.WriteLine(BitConverter.ToString(e.Payload.Buffer.ToArray()));
            };

            transport.ReceivedPayload += (s, e) =>
            {
                Console.WriteLine($"RECV DLP");
                Console.WriteLine(BitConverter.ToString(e.Payload.Buffer.ToArray()));
            };*/



            /*DlpConnection dlp = new DlpConnection(transport);

            for (int i = 0; i < 1; i++)
            {
                DlpArgumentCollection readUserInfoRequestArgs = new DlpArgumentCollection();
                DlpArgumentCollection readUserInfoResponseArgs = await dlp.Execute(DlpCommandDefinitions.ReadUserInfo, readUserInfoRequestArgs);
            }

            DlpArgumentCollection readSysInfoRequestArgs = new DlpArgumentCollection();
            readSysInfoRequestArgs.SetValue(DlpCommandDefinitions.ReadSysInfoArgs.ReadSysInfoRequest, new ReadSysInfoRequest
            {
                HostDlpVersionMajor = 1,
                HostDlpVersionMinor = 4,
            });
            DlpArgumentCollection readSysInfoResponseArgs = await dlp.Execute(DlpCommandDefinitions.ReadSysInfo, readSysInfoRequestArgs);

            var result = await ReadDatabaseMetadata(dlp);

            DlpArgumentCollection endOfSyncRequestArgs = new DlpArgumentCollection();
            endOfSyncRequestArgs.SetValue(DlpCommandDefinitions.EndOfSyncArgs.EndOfSyncRequest, new EndOfSyncRequest()
            {
                Status = EndOfSyncRequest.EndOfSyncStatus.Okay
            });
            await dlp.Execute(DlpCommandDefinitions.EndOfSync, endOfSyncRequestArgs);

            await ioTask;

            Console.ReadLine();*/
        }

        private static async Task<List<DlpDatabaseMetadata>> ReadDatabaseMetadata(DlpConnection dlp)
        {
            List<DlpDatabaseMetadata> results = new List<DlpDatabaseMetadata>();
            bool gotData = true;
            while (gotData)
            {
                DlpArgumentCollection requestArgs = new DlpArgumentCollection();
                requestArgs.SetValue(DlpCommandDefinitions.ReadDbListArgs.ReadDbListRequest, new ReadDbListRequest()
                {
                    Mode = ReadDbListRequest.ReadDbListMode.ListRam | ReadDbListRequest.ReadDbListMode.ListMultiple,
                    CardId = 0,
                    StartIndex = results.Any() ? (ushort)(results.Max(r => r.Index) + 1) : (ushort)0,
                });

                try
                {
                    DlpArgumentCollection responseArgs = await dlp.Execute(DlpCommandDefinitions.ReadDbList, requestArgs);

                    ReadDbListResponse? response = responseArgs.GetValue(DlpCommandDefinitions.ReadDbListArgs.ReadDbListResponse);

                    if (response == null)
                        throw new Exception("Didn't get expected response argument");

                    results.AddRange(response.Metadata);

                    if (response.Metadata.Length == 0)
                        gotData = false;
                }
                catch (DlpCommandErrorException ex)
                {
                    if (ex.ErrorCode == DlpErrorCode.NotFoundError)
                        break;

                    throw;
                }
            }

            return results;
        }
    }
}