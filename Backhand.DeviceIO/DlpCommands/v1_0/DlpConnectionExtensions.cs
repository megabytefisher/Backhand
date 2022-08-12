using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0
{
    public static class DlpConnectionExtensions
    {
        public static async Task<ReadUserInfoResponse> ReadUserInfo(this DlpConnection dlp, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadUserInfo, requestArguments, cancellationToken);

            ReadUserInfoResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadUserInfoResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task<(ReadSysInfoResponse, ReadSysInfoDlpVersionsResponse)> ReadSysInfo(this DlpConnection dlp, ReadSysInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
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

        public static async Task<ReadDbListResponse> ReadDbList(this DlpConnection dlp, ReadDbListRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
            requestArguments.SetValue(DlpArgumentDefinitions.ReadDbListRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadDbList, requestArguments, cancellationToken);

            ReadDbListResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadDbListResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task<OpenDbResponse> OpenDb(this DlpConnection dlp, OpenDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
            requestArguments.SetValue(DlpArgumentDefinitions.OpenDbRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.OpenDb, requestArguments, cancellationToken);

            OpenDbResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.OpenDbResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task<CreateDbResponse> CreateDb(this DlpConnection dlp, CreateDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
            requestArguments.SetValue(DlpArgumentDefinitions.CreateDbRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.CreateDb, requestArguments, cancellationToken);

            CreateDbResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.CreateDbResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task CloseDb(this DlpConnection dlp, CloseDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
            requestArguments.SetValue(DlpArgumentDefinitions.CloseDbRequest, request);

            await dlp.Execute(DlpCommandDefinitions.CloseDb, requestArguments, cancellationToken);
        }

        public static async Task<ReadAppBlockResponse> ReadAppBlock(this DlpConnection dlp, ReadAppBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
            requestArguments.SetValue(DlpArgumentDefinitions.ReadAppBlockRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadAppBlock, requestArguments, cancellationToken);

            ReadAppBlockResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadAppBlockResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task WriteAppBlock(this DlpConnection dlp, WriteAppBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
            requestArguments.SetValue(DlpArgumentDefinitions.WriteAppBlockRequest, request);

            await dlp.Execute(DlpCommandDefinitions.WriteAppBlock, requestArguments, cancellationToken);
        }

        public static async Task<ReadSortBlockResponse> ReadSortBlock(this DlpConnection dlp, ReadSortBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
            requestArguments.SetValue(DlpArgumentDefinitions.ReadSortBlockRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadSortBlock, requestArguments, cancellationToken);

            ReadSortBlockResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadSortBlockResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task WriteSortBlock(this DlpConnection dlp, WriteSortBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
            requestArguments.SetValue(DlpArgumentDefinitions.WriteSortBlockRequest, request);

            await dlp.Execute(DlpCommandDefinitions.WriteSortBlock, requestArguments, cancellationToken);
        }

        public static async Task<ReadRecordByIdResponse> ReadRecordById(this DlpConnection dlp, ReadRecordByIdRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
            requestArguments.SetValue(DlpArgumentDefinitions.ReadRecordByIdRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadRecordById, requestArguments, cancellationToken);

            ReadRecordByIdResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadRecordByIdResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task<ReadResourceByIndexResponse> ReadResourceByIndex(this DlpConnection dlp, ReadResourceByIndexRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
            requestArguments.SetValue(DlpArgumentDefinitions.ReadResourceByIndexRequest, request);

            DlpArgumentCollection responseArguments = await dlp.Execute(DlpCommandDefinitions.ReadResourceByIndex, requestArguments, cancellationToken);

            ReadResourceByIndexResponse? response =
                responseArguments.GetValue(DlpArgumentDefinitions.ReadResourceByIndexResponse);

            if (response == null)
                throw new DlpCommandException("Missing required response argument");

            return response;
        }

        public static async Task WriteResource(this DlpConnection dlp, WriteResourceRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
            requestArguments.SetValue(DlpArgumentDefinitions.WriteResourceRequest, request);

            await dlp.Execute(DlpCommandDefinitions.WriteResource, requestArguments, cancellationToken);
        }

        public static async Task OpenConduit(this DlpConnection dlp, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();

            await dlp.Execute(DlpCommandDefinitions.OpenConduit, requestArguments, cancellationToken);
        }

        public static async Task EndOfSync(this DlpConnection dlp, EndOfSyncRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
            requestArguments.SetValue(DlpArgumentDefinitions.EndOfSyncRequest, request);

            await dlp.Execute(DlpCommandDefinitions.EndOfSync, requestArguments, cancellationToken);
        }

        public static async Task<ReadRecordIdListResponse> ReadRecordIdList(this DlpConnection dlp, ReadRecordIdListRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection requestArguments = new DlpArgumentCollection();
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
