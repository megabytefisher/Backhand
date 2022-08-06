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
