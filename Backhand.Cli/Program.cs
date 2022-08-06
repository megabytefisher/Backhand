using Backhand.DeviceIO.Cmp;
using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using Backhand.DeviceIO.DlpServers;
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
            Func<DlpConnection, CancellationToken, Task> syncFunc = async (dlp, cancellationToken) =>
            {
                (ReadSysInfoResponse readSysInforesponse, ReadSysInfoDlpVersionsResponse dlpVersionsResponse) =
                    await dlp.ReadSysInfo(new ReadSysInfoRequest()
                    {
                        HostDlpVersionMajor = 1,
                        HostDlpVersionMinor = 4,
                    });

                ReadDbListResponse readDbListResponse =
                    await dlp.ReadDbList(new ReadDbListRequest()
                    {
                        CardId = 0,
                        Mode = ReadDbListRequest.ReadDbListMode.ListRam | ReadDbListRequest.ReadDbListMode.ListMultiple,
                        StartIndex = 0
                    });

                OpenDbResponse openDbResponse =
                    await dlp.OpenDb(new OpenDbRequest()
                    {
                        CardId = 0,
                        Mode = OpenDbRequest.OpenDbMode.Read,
                        Name = "AddressDB"
                    });

                ReadRecordIdListResponse readRecordIdListResponse =
                    await dlp.ReadRecordIdList(new ReadRecordIdListRequest()
                    {
                        DbHandle = openDbResponse.DbHandle,
                        MaxRecords = 100,
                        StartIndex = 0,
                    });


                Console.WriteLine("OK");
            };

            //SerialDlpServer server = new SerialDlpServer("COM3", syncFunc);
            UsbDlpServer server = new UsbDlpServer(syncFunc);

            await server.Run();
        }
    }
}