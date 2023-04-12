using Backhand.Protocols.Dlp;
using Backhand.Dlp.Commands.v1_0.Arguments;

namespace Backhand.Dlp.Commands.v1_0
{
    public static class DlpConnectionExtensions
    {
        public static async Task<ReadUserInfoResponse> ReadUserInfoAsync(this DlpConnection connection)
        {
            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.ReadUserInfo);

            return responseArguments.GetValue(DlpCommands.ReadUserInfoArguments.Result) ?? throw new System.Exception("Failed to get ReadUserInfoResult");
        }

        public static async Task EndOfSyncAsync(this DlpConnection connection, EndOfSyncRequest request)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.EndOfSyncArguments.Request, request);

            await connection.ExecuteTransactionAsync(DlpCommands.EndOfSync, requestArguments);
        }
    }
}
