using System;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands.v1_2.Arguments;
using Backhand.Protocols.Dlp;
using static Backhand.Dlp.Commands.v1_2.DlpCommands;
using ReadDbListRequest = Backhand.Dlp.Commands.v1_2.Arguments.ReadDbListRequest;

namespace Backhand.Dlp.Commands.v1_2
{
    public class DlpClient : v1_1.DlpClient
    {
        public DlpClient(DlpConnection connection) : base(connection)
        {
        }
        
        public async Task<ReadDbListResponse> ReadDbListAsync(ReadDbListRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadDbListArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadDbList,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(ReadDbListArguments.Response) ?? throw new Exception("Failed to get ReadDbListResponse");
        }
        
        public async Task<(ReadSysInfoSystemResponse, ReadSysInfoDlpResponse)> ReadSysInfoAsync(ReadSysInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadSysInfoArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadSysInfo,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return (
                responseArguments.GetValue(ReadSysInfoArguments.SystemResponse) ?? throw new Exception("Failed to get ReadSysInfoSystemResponse"),
                responseArguments.GetValue(ReadSysInfoArguments.DlpResponse) ?? throw new Exception("Failed to get ReadSysInfoDlpResponse")
            );
        }
    }
}