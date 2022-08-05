using Backhand.DeviceIO.Cmp;
using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.Dlp.Arguments;
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
            USBDeviceInfo deviceInfo = USBDevice.GetDevices("dee824ef-729b-4a0e-9c14-b7117d33a817").FirstOrDefault();
            WindowsUsbNetSyncDevice netSyncDevice = new WindowsUsbNetSyncDevice(deviceInfo);
            /*netSyncDevice.ReceivedPacket += (s, e) =>
            {
                Console.WriteLine($"RECV NetSync ({e.Packet.TransactionId}):");
                Console.WriteLine(BitConverter.ToString(e.Packet.Data.ToArray()));
            };*/

            Task ioTask = netSyncDevice.RunIOAsync();
            await netSyncDevice.DoNetSyncHandshake();

            NetSyncDlpTransport transport = new NetSyncDlpTransport(netSyncDevice);

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



            DlpConnection dlp = new DlpConnection(transport);

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

            Console.ReadLine();







            /*using SlpDevice slp = new SlpDevice("COM3");

            slp.ReceivedPacket += (s, e) =>
            {
                Console.WriteLine("RECV");
                Console.WriteLine(e.Packet);
                Console.WriteLine();
            };

            slp.SendingPacket += (s, e) =>
            {
                Console.WriteLine("SEND");
                Console.WriteLine(e.Packet);
                Console.WriteLine();
            };

            using PadpConnection padp = new PadpConnection(slp, 3, 3, 0xff);

            CmpConnection cmp = new CmpConnection(padp);

            Task ioTask = slp.RunIOAsync();

            await cmp.DoHandshakeAsync();

            DlpConnection dlp = new DlpConnection(padp);

            DlpArgumentCollection readUserInfoRequestArgs = new DlpArgumentCollection();
            DlpArgumentCollection readUserInfoResponseArgs = await dlp.Execute(DlpCommandDefinitions.ReadUserInfo, readUserInfoRequestArgs);


            DlpArgumentCollection readSysInfoRequestArgs = new DlpArgumentCollection();
            readSysInfoRequestArgs.SetValue(DlpCommandDefinitions.ReadSysInfoArgs.ReadSysInfoRequest, new ReadSysInfoRequest
            {
                HostDlpVersionMajor = 1,
                HostDlpVersionMinor = 4,
            });
            DlpArgumentCollection readSysInfoResponseArgs = await dlp.Execute(DlpCommandDefinitions.ReadSysInfo, readSysInfoRequestArgs);

            //DlpArgumentCollection requestArgs = new DlpArgumentCollection();
            //requestArgs.SetValue(DlpCommandDefinitions.ReadDbListArgs.ReadDbListRequest, new ReadDbListRequest()
            //{
            //    Mode = ReadDbListRequest.ReadDbListMode.ListRam,
            //    CardId = 0,
            //    StartIndex = 0
            //});

            DlpArgumentCollection responseArgs = await dlp.Execute(DlpCommandDefinitions.ReadDbList, requestArgs);

            //List<DlpDatabaseMetadata> metadata = await ReadDatabaseMetadata(dlp);

            DlpArgumentCollection endOfSyncRequestArgs = new DlpArgumentCollection();
            endOfSyncRequestArgs.SetValue(DlpCommandDefinitions.EndOfSyncArgs.EndOfSyncRequest, new EndOfSyncRequest()
            {
                Status = EndOfSyncRequest.EndOfSyncStatus.Okay
            });
            await dlp.Execute(DlpCommandDefinitions.EndOfSync, endOfSyncRequestArgs);

            await ioTask;*/
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

                    ReadDbListResponse? response = responseArgs.GetValue<ReadDbListResponse>(DlpCommandDefinitions.ReadDbListArgs.ReadDbListResponse);

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