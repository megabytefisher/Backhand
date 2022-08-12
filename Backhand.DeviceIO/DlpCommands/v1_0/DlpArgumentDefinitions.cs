using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0
{
    public static class DlpArgumentDefinitions
    {
        /* ReadUserInfo */
        public static readonly DlpArgumentDefinition<ReadUserInfoResponse> ReadUserInfoResponse = new DlpArgumentDefinition<ReadUserInfoResponse>();

        /* ReadSysInfo */
        public static readonly DlpArgumentDefinition<ReadSysInfoRequest> ReadSysInfoRequest = new DlpArgumentDefinition<ReadSysInfoRequest>();
        public static readonly DlpArgumentDefinition<ReadSysInfoResponse> ReadSysInfoResponse = new DlpArgumentDefinition<ReadSysInfoResponse>();
        public static readonly DlpArgumentDefinition<ReadSysInfoDlpVersionsResponse> ReadSysInfoDlpVersionsResponse = new DlpArgumentDefinition<ReadSysInfoDlpVersionsResponse>();

        /* ReadDbList */
        public static readonly DlpArgumentDefinition<ReadDbListRequest> ReadDbListRequest = new DlpArgumentDefinition<ReadDbListRequest>();
        public static readonly DlpArgumentDefinition<ReadDbListResponse> ReadDbListResponse = new DlpArgumentDefinition<ReadDbListResponse>();

        /* OpenDb */
        public static readonly DlpArgumentDefinition<OpenDbRequest> OpenDbRequest = new DlpArgumentDefinition<OpenDbRequest>();
        public static readonly DlpArgumentDefinition<OpenDbResponse> OpenDbResponse = new DlpArgumentDefinition<OpenDbResponse>();

        /* CreateDb */
        public static readonly DlpArgumentDefinition<CreateDbRequest> CreateDbRequest = new DlpArgumentDefinition<CreateDbRequest>();
        public static readonly DlpArgumentDefinition<CreateDbResponse> CreateDbResponse = new DlpArgumentDefinition<CreateDbResponse>();

        /* CloseDb */
        public static readonly DlpArgumentDefinition<CloseDbRequest> CloseDbRequest = new DlpArgumentDefinition<CloseDbRequest>();

        /* DeleteDb */
        public static readonly DlpArgumentDefinition<DeleteDbRequest> DeleteDbRequest = new DlpArgumentDefinition<DeleteDbRequest>();

        /* ReadAppBlock */
        public static readonly DlpArgumentDefinition<ReadAppBlockRequest> ReadAppBlockRequest = new DlpArgumentDefinition<ReadAppBlockRequest>();
        public static readonly DlpArgumentDefinition<ReadAppBlockResponse> ReadAppBlockResponse = new DlpArgumentDefinition<ReadAppBlockResponse>();

        /* ReadSortBlock */
        public static readonly DlpArgumentDefinition<WriteAppBlockRequest> WriteAppBlockRequest = new DlpArgumentDefinition<WriteAppBlockRequest>();

        /* ReadSortBlock */
        public static readonly DlpArgumentDefinition<ReadSortBlockRequest> ReadSortBlockRequest = new DlpArgumentDefinition<ReadSortBlockRequest>();
        public static readonly DlpArgumentDefinition<ReadSortBlockResponse> ReadSortBlockResponse = new DlpArgumentDefinition<ReadSortBlockResponse>();

        /* WriteSortBlock */
        public static readonly DlpArgumentDefinition<WriteSortBlockRequest> WriteSortBlockRequest = new DlpArgumentDefinition<WriteSortBlockRequest>();

        /* ReadRecordById */
        public static readonly DlpArgumentDefinition<ReadRecordByIdRequest> ReadRecordByIdRequest = new DlpArgumentDefinition<ReadRecordByIdRequest>();
        public static readonly DlpArgumentDefinition<ReadRecordByIdResponse> ReadRecordByIdResponse = new DlpArgumentDefinition<ReadRecordByIdResponse>();

        /* WriteRecord */
        public static readonly DlpArgumentDefinition<WriteRecordRequest> WriteRecordRequest = new DlpArgumentDefinition<WriteRecordRequest>();
        public static readonly DlpArgumentDefinition<WriteRecordResponse> WriteRecordResponse = new DlpArgumentDefinition<WriteRecordResponse>();

        /* ReadResourceByIndex */
        public static readonly DlpArgumentDefinition<ReadResourceByIndexRequest> ReadResourceByIndexRequest = new DlpArgumentDefinition<ReadResourceByIndexRequest>();
        public static readonly DlpArgumentDefinition<ReadResourceByIndexResponse> ReadResourceByIndexResponse = new DlpArgumentDefinition<ReadResourceByIndexResponse>();

        /* WriteResource */
        public static readonly DlpArgumentDefinition<WriteResourceRequest> WriteResourceRequest = new DlpArgumentDefinition<WriteResourceRequest>();

        /* EndOfSync */
        public static readonly DlpArgumentDefinition<EndOfSyncRequest> EndOfSyncRequest = new DlpArgumentDefinition<EndOfSyncRequest>();

        /* ReadRecordIdList */
        public static readonly DlpArgumentDefinition<ReadRecordIdListRequest> ReadRecordIdListRequest = new DlpArgumentDefinition<ReadRecordIdListRequest>();
        public static readonly DlpArgumentDefinition<ReadRecordIdListResponse> ReadRecordIdListResponse = new DlpArgumentDefinition<ReadRecordIdListResponse>();
    }
}
