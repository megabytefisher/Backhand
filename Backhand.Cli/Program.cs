using Backhand.DeviceIO.Cmp;
using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.Dlp.Arguments;
using Backhand.DeviceIO.Padp;
using Backhand.DeviceIO.Slp;

namespace Backhand.Cli
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using SlpDevice slp = new SlpDevice("COM3");

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

            /*DlpArgumentCollection requestArgs = new DlpArgumentCollection();
            requestArgs.SetValue(DlpCommandDefinitions.ReadDbListArgs.ReadDbListRequest, new ReadDbListRequest()
            {
                Mode = ReadDbListRequest.ReadDbListMode.ListRam,
                CardId = 0,
                StartIndex = 0
            });

            DlpArgumentCollection responseArgs = await dlp.Execute(DlpCommandDefinitions.ReadDbList, requestArgs);*/

            //List<DlpDatabaseMetadata> metadata = await ReadDatabaseMetadata(dlp);

            DlpArgumentCollection endOfSyncRequestArgs = new DlpArgumentCollection();
            endOfSyncRequestArgs.SetValue(DlpCommandDefinitions.EndOfSyncArgs.EndOfSyncRequest, new EndOfSyncRequest()
            {
                Status = EndOfSyncRequest.EndOfSyncStatus.Okay
            });
            await dlp.Execute(DlpCommandDefinitions.EndOfSync, endOfSyncRequestArgs);

            await ioTask;
        }

        private static async Task<List<DlpDatabaseMetadata>> ReadDatabaseMetadata(DlpConnection dlp)
        {
            List<DlpDatabaseMetadata> results = new List<DlpDatabaseMetadata>();
            bool gotData = true;
            ushort index = 0;
            while (gotData)
            {
                DlpArgumentCollection requestArgs = new DlpArgumentCollection();
                requestArgs.SetValue(DlpCommandDefinitions.ReadDbListArgs.ReadDbListRequest, new ReadDbListRequest()
                {
                    Mode = ReadDbListRequest.ReadDbListMode.ListRam,
                    CardId = 0,
                    StartIndex = index
                });

                try
                {
                    DlpArgumentCollection responseArgs = await dlp.Execute(DlpCommandDefinitions.ReadDbList, requestArgs);

                    ReadDbListResponse? response = responseArgs.GetValue<ReadDbListResponse>(DlpCommandDefinitions.ReadDbListArgs.ReadDbListResponse);

                    if (response == null)
                        throw new Exception("Didn't get expected response argument");

                    results.AddRange(response.Metadata);

                    index++;

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