using Backhand.DeviceIO.Cmp;
using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using Backhand.DeviceIO.DlpCommands.v1_0.Data;
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
                Console.WriteLine("Starting sync");
                try
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

                    string dbName = readDbListResponse.Metadata.First(m => m.Flags.HasFlag(DatabaseMetadata.DatabaseFlags.ResourceDb)).Name;

                    OpenDbResponse openDbResponse =
                        await dlp.OpenDb(new OpenDbRequest()
                        {
                            CardId = 0,
                            Mode = OpenDbRequest.OpenDbMode.Read,
                            Name = dbName
                        });

                    ReadResourceByIndexResponse readResourceByIndexResponse =
                        await dlp.ReadResourceByIndex(new ReadResourceByIndexRequest()
                        {
                            DbHandle = openDbResponse.DbHandle,
                            ResourceIndex = i,
                            Offset = 0,
                            MaxLength = 0xff
                        });

                    /*ReadRecordIdListResponse readRecordIdListResponse =
                        await dlp.ReadRecordIdList(new ReadRecordIdListRequest()
                        {
                            DbHandle = openDbResponse.DbHandle,
                            MaxRecords = 100,
                            StartIndex = 0,
                        });

                    ReadRecordByIdResponse readRecordByIdResponse =
                        await dlp.ReadRecordById(new ReadRecordByIdRequest()
                        {
                            DbHandle = openDbResponse.DbHandle,
                            RecordId = readRecordIdListResponse.RecordIds.First(),
                            Offset = 0,
                            MaxLength = 0xff
                        });*/


                    Console.WriteLine("Sync OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Sync failed");
                    Console.WriteLine(ex);
                }
                
            };

            SerialDlpServer server = new SerialDlpServer("COM3", syncFunc);
            //UsbDlpServer server = new UsbDlpServer(syncFunc);

            await server.Run();
        }
    }
}