using System;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands.v1_1.Arguments;
using Backhand.Protocols.Dlp;
using static Backhand.Dlp.Commands.v1_1.DlpCommands;

namespace Backhand.Dlp.Commands.v1_1
{
    public class DlpClient : v1_0.DlpClient
    {
        public DlpClient(DlpConnection connection) : base(connection)
        {
        }

        public async Task<ReadNextRecInCategoryResponse> ReadNextRecInCategoryAsync(ReadNextRecInCategoryRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadNextRecInCategoryArguments.Request, request);

            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadNextRecInCategory,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);

            return responseArguments.GetValue(ReadNextRecInCategoryArguments.Response) ?? throw new Exception("Failed to get ReadNextRecInCategoryResponse");
        }
    }
}