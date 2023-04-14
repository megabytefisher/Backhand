using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp.Commands.v1_0
{
    public static class DlpConnectionExtensions
    {
        public static async Task<ReadUserInfoResponse> ReadUserInfoAsync(this DlpConnection connection, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.ReadUserInfo, cancellationToken: cancellationToken);

            return responseArguments.GetValue(DlpCommands.ReadUserInfoArguments.Response) ?? throw new Exception("Failed to get ReadUserInfoResult");
        }

        public static async Task<(ReadSysInfoSystemResponse, ReadSysInfoDlpResponse)> ReadSysInfoAsync(this DlpConnection connection, ReadSysInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.ReadSysInfoArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.ReadSysInfo, requestArguments, cancellationToken);

            return (
                responseArguments.GetValue(DlpCommands.ReadSysInfoArguments.SystemResponse) ?? throw new Exception("Failed to get ReadSysInfoSystemResult"),
                responseArguments.GetValue(DlpCommands.ReadSysInfoArguments.DlpResponse) ?? throw new Exception("Failed to get ReadSysInfoDlpResult")
            );
        }

        public static async Task<ReadDbListResponse> ReadDbListAsync(this DlpConnection connection, ReadDbListRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.ReadDbListArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.ReadDbList, requestArguments, cancellationToken);

            return responseArguments.GetValue(DlpCommands.ReadDbListArguments.Response) ?? throw new Exception("Failed to get ReadDbListResult");
        }

        public static async Task<CreateDbResponse> CreateDbAsync(this DlpConnection connection, CreateDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.CreateDbArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.CreateDb, requestArguments, cancellationToken);
            return responseArguments.GetValue(DlpCommands.CreateDbArguments.Response) ?? throw new Exception("Failed to get CreateDbResult");
        }

        public static async Task CloseDbAsync(this DlpConnection connection, CloseDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.CloseDbArguments.Request, request);

            await connection.ExecuteTransactionAsync(DlpCommands.CloseDb, requestArguments, cancellationToken);
        }

        public static async Task OpenConduitAsync(this DlpConnection connection, CancellationToken cancellationToken = default)
        {
            await connection.ExecuteTransactionAsync(DlpCommands.OpenConduit, cancellationToken: cancellationToken);
        }

        public static async Task EndSyncAsync(this DlpConnection connection, EndSyncRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.EndSyncArguments.Request, request);

            await connection.ExecuteTransactionAsync(DlpCommands.EndSync, requestArguments, cancellationToken);
        }
    }
}
