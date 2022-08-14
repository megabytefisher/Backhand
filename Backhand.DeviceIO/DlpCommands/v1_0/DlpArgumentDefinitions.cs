using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;

namespace Backhand.DeviceIO.DlpCommands.v1_0
{
    public static class DlpArgumentDefinitions
    {
        /* ReadUserInfo */
        public static readonly DlpArgumentDefinition<ReadUserInfoResponse> ReadUserInfoResponse = new();

        /* ReadSysInfo */
        public static readonly DlpArgumentDefinition<ReadSysInfoRequest> ReadSysInfoRequest = new();
        public static readonly DlpArgumentDefinition<ReadSysInfoResponse> ReadSysInfoResponse = new();
        public static readonly DlpArgumentDefinition<ReadSysInfoDlpVersionsResponse> ReadSysInfoDlpVersionsResponse = new();

        /* ReadDbList */
        public static readonly DlpArgumentDefinition<ReadDbListRequest> ReadDbListRequest = new();
        public static readonly DlpArgumentDefinition<ReadDbListResponse> ReadDbListResponse = new();

        /* OpenDb */
        public static readonly DlpArgumentDefinition<OpenDbRequest> OpenDbRequest = new();
        public static readonly DlpArgumentDefinition<OpenDbResponse> OpenDbResponse = new();

        /* CreateDb */
        public static readonly DlpArgumentDefinition<CreateDbRequest> CreateDbRequest = new();
        public static readonly DlpArgumentDefinition<CreateDbResponse> CreateDbResponse = new();

        /* CloseDb */
        public static readonly DlpArgumentDefinition<CloseDbRequest> CloseDbRequest = new();

        /* DeleteDb */
        public static readonly DlpArgumentDefinition<DeleteDbRequest> DeleteDbRequest = new();

        /* ReadAppBlock */
        public static readonly DlpArgumentDefinition<ReadAppBlockRequest> ReadAppBlockRequest = new();
        public static readonly DlpArgumentDefinition<ReadAppBlockResponse> ReadAppBlockResponse = new();

        /* ReadSortBlock */
        public static readonly DlpArgumentDefinition<WriteAppBlockRequest> WriteAppBlockRequest = new();

        /* ReadSortBlock */
        public static readonly DlpArgumentDefinition<ReadSortBlockRequest> ReadSortBlockRequest = new();
        public static readonly DlpArgumentDefinition<ReadSortBlockResponse> ReadSortBlockResponse = new();

        /* WriteSortBlock */
        public static readonly DlpArgumentDefinition<WriteSortBlockRequest> WriteSortBlockRequest = new();

        /* ReadRecordById */
        public static readonly DlpArgumentDefinition<ReadRecordByIdRequest> ReadRecordByIdRequest = new();
        public static readonly DlpArgumentDefinition<ReadRecordByIdResponse> ReadRecordByIdResponse = new();

        /* WriteRecord */
        public static readonly DlpArgumentDefinition<WriteRecordRequest> WriteRecordRequest = new();
        public static readonly DlpArgumentDefinition<WriteRecordResponse> WriteRecordResponse = new();

        /* ReadResourceByIndex */
        public static readonly DlpArgumentDefinition<ReadResourceByIndexRequest> ReadResourceByIndexRequest = new();
        public static readonly DlpArgumentDefinition<ReadResourceByIndexResponse> ReadResourceByIndexResponse = new();

        /* WriteResource */
        public static readonly DlpArgumentDefinition<WriteResourceRequest> WriteResourceRequest = new();

        /* EndOfSync */
        public static readonly DlpArgumentDefinition<EndOfSyncRequest> EndOfSyncRequest = new();

        /* ReadRecordIdList */
        public static readonly DlpArgumentDefinition<ReadRecordIdListRequest> ReadRecordIdListRequest = new();
        public static readonly DlpArgumentDefinition<ReadRecordIdListResponse> ReadRecordIdListResponse = new();
    }
}
