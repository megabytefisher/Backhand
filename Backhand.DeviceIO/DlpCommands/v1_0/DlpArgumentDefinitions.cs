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

        /* EndOfSync */
        public static readonly DlpArgumentDefinition<EndOfSyncRequest> EndOfSyncRequest = new DlpArgumentDefinition<EndOfSyncRequest>();

        /* ReadRecordIdList */
        public static readonly DlpArgumentDefinition<ReadRecordIdListRequest> ReadRecordIdListRequest = new DlpArgumentDefinition<ReadRecordIdListRequest>();
        public static readonly DlpArgumentDefinition<ReadRecordIdListResponse> ReadRecordIdListResponse = new DlpArgumentDefinition<ReadRecordIdListResponse>();
    }
}
