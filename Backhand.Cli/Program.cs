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
using Backhand.Pdb;
using MadWizard.WinUSBNet;
using System.Buffers;

namespace Backhand.Cli
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using FileStream inStream = new FileStream("C:\\Users\\kfisher\\Documents\\Palm OS Desktop\\Kevin\\Backup\\_HaCkMe_.PRC", FileMode.Open, FileAccess.Read, FileShare.None, 1024, true);
            //using FileStream outStream = new FileStream("testout.prc", FileMode.Create, FileAccess.Write, FileShare.None, 1024, true);
            ResourceDatabase database = new ResourceDatabase();
            await database.Deserialize(inStream);
            //await database.Serialize(outStream);

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

                    CreateDbResponse createDbResponse =
                        await dlp.CreateDb(new CreateDbRequest()
                        {
                            Type = database.Header.Type,
                            Creator = database.Header.Creator,
                            Attributes = (DlpDatabaseAttributes)database.Header.Attributes,
                            CardId = 0,
                            Version = database.Header.Version,
                            Name = database.Header.Name
                        });

                    foreach (DatabaseResource resource in database.Resources.Skip(2))
                    {
                        await dlp.WriteResource(new WriteResourceRequest()
                        {
                            DbHandle = createDbResponse.DbHandle,
                            Type = resource.Type,
                            ResourceId = resource.ResourceId,
                            Size = Convert.ToUInt16(resource.Data.Length),
                            Data = resource.Data,
                        });
                    }

                    await dlp.CloseDb(new CloseDbRequest()
                    {
                        DbHandle = createDbResponse.DbHandle
                    });

                    /*ReadDbListResponse readDbListResponse =
                        await dlp.ReadDbList(new ReadDbListRequest()
                        {
                            CardId = 0,
                            Mode = ReadDbListRequest.ReadDbListMode.ListRam | ReadDbListRequest.ReadDbListMode.ListMultiple,
                            StartIndex = 0
                        });

                    string dbName = readDbListResponse.Metadata.First(m => m.Attributes.HasFlag(DlpDatabaseAttributes.ResourceDb)).Name;

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
                            ResourceIndex = 0,
                            Offset = 0,
                            MaxLength = 0xff
                        });

                    ReadRecordIdListResponse readRecordIdListResponse =
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