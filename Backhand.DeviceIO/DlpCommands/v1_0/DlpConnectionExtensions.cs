using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0
{
    public static class DlpConnectionExtensions
    {
        public static async Task<ReadUserInfoResponse> ReadUserInfoAsync(this DlpConnection dlp, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadUserInfo, requestArguments, cancellationToken);

            ReadUserInfoResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadUserInfoResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task<(ReadSysInfoResponse, ReadSysInfoDlpVersionsResponse)> ReadSysInfoAsync(this DlpConnection dlp, ReadSysInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.ReadSysInfoRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadSysInfo, requestArguments, cancellationToken);

            ReadSysInfoResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadSysInfoResponse);
            ReadSysInfoDlpVersionsResponse? dlpVersionsResponse =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadSysInfoDlpVersionsResponse);

            if (response == null || dlpVersionsResponse == null)
                throw new DlpCommandException("Missing required response argument");

            return (response, dlpVersionsResponse);
        }

        public static async Task<ReadDbListResponse> ReadDbListAsync(this DlpConnection dlp, ReadDbListRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.ReadDbListRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadDbList, requestArguments, cancellationToken);

            ReadDbListResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadDbListResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task<OpenDbResponse> OpenDbAsync(this DlpConnection dlp, OpenDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.OpenDbRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.OpenDb, requestArguments, cancellationToken);

            OpenDbResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.OpenDbResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task<CreateDbResponse> CreateDbAsync(this DlpConnection dlp, CreateDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.CreateDbRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.CreateDb, requestArguments, cancellationToken);

            CreateDbResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.CreateDbResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task CloseDbAsync(this DlpConnection dlp, CloseDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.CloseDbRequest, request);

            await dlp.Execute(DlpCommandDefinitions.CloseDb, requestArguments, cancellationToken);
        }

        public static async Task DeleteDbAsync(this DlpConnection dlp, DeleteDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.DeleteDbRequest, request);

            await dlp.Execute(DlpCommandDefinitions.DeleteDb, requestArguments, cancellationToken);
        }

        public static async Task<ReadAppBlockResponse> ReadAppBlockAsync(this DlpConnection dlp, ReadAppBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.ReadAppBlockRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadAppBlock, requestArguments, cancellationToken);

            ReadAppBlockResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadAppBlockResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task WriteAppBlockAsync(this DlpConnection dlp, WriteAppBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.WriteAppBlockRequest, request);

            await dlp.Execute(DlpCommandDefinitions.WriteAppBlock, requestArguments, cancellationToken);
        }

        public static async Task<ReadSortBlockResponse> ReadSortBlockAsync(this DlpConnection dlp, ReadSortBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.ReadSortBlockRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadSortBlock, requestArguments, cancellationToken);

            ReadSortBlockResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadSortBlockResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task WriteSortBlockAsync(this DlpConnection dlp, WriteSortBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.WriteSortBlockRequest, request);

            await dlp.Execute(DlpCommandDefinitions.WriteSortBlock, requestArguments, cancellationToken);
        }

        public static async Task<ReadRecordByIdResponse> ReadRecordByIdAsync(this DlpConnection dlp, ReadRecordByIdRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.ReadRecordByIdRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadRecordById, requestArguments, cancellationToken);

            ReadRecordByIdResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadRecordByIdResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task<WriteRecordResponse> WriteRecordAsync(this DlpConnection dlp, WriteRecordRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.WriteRecordRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.WriteRecord, requestArguments, cancellationToken);

            WriteRecordResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.WriteRecordResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task<ReadResourceByIndexResponse> ReadResourceByIndexAsync(this DlpConnection dlp, ReadResourceByIndexRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.ReadResourceByIndexRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadResourceByIndex, requestArguments, cancellationToken);

            ReadResourceByIndexResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadResourceByIndexResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task WriteResourceAsync(this DlpConnection dlp, WriteResourceRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.WriteResourceRequest, request);

            await dlp.Execute(DlpCommandDefinitions.WriteResource, requestArguments, cancellationToken);
        }

        public static async Task OpenConduitAsync(this DlpConnection dlp, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();

            await dlp.Execute(DlpCommandDefinitions.OpenConduit, requestArguments, cancellationToken);
        }

        public static async Task EndOfSyncAsync(this DlpConnection dlp, EndOfSyncRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.EndOfSyncRequest, request);

            await dlp.Execute(DlpCommandDefinitions.EndOfSync, requestArguments, cancellationToken);
        }

        public static async Task<ReadRecordIdListResponse> ReadRecordIdListAsync(this DlpConnection dlp, ReadRecordIdListRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new();
            requestArguments.SetValue(DlpArgumentDefinitions.ReadRecordIdListRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadRecordIdList, requestArguments, cancellationToken);

            ReadRecordIdListResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadRecordIdListResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }
    }
}
