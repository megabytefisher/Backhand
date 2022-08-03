using Backhand.DeviceIO.Dlp.Arguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp
{
    public class DlpCommandDefinitions
    {
        public static class ReadUserInfoArgs
        {
            public static readonly DlpArgumentDefinition<UserInfoResponse> UserInfoResponse = new DlpArgumentDefinition<UserInfoResponse>(false);
        }

        public static readonly DlpCommandDefinition ReadUserInfo = new DlpCommandDefinition(
            DlpOpcode.ReadUserInfo,
            new DlpArgumentDefinition[] { },
            new DlpArgumentDefinition[] { ReadUserInfoArgs.UserInfoResponse });

        public static class ReadSysInfoArgs
        {
            public static readonly DlpArgumentDefinition<ReadSysInfoRequest> ReadSysInfoRequest = new DlpArgumentDefinition<ReadSysInfoRequest>();

            public static readonly DlpArgumentDefinition<ReadSysInfoResponse> ReadSysInfoResponse = new DlpArgumentDefinition<ReadSysInfoResponse>();
            public static readonly DlpArgumentDefinition<ReadSysInfoDlpResponse> ReadSysInfoDlpResponse = new DlpArgumentDefinition<ReadSysInfoDlpResponse>();
        }

        public static readonly DlpCommandDefinition ReadSysInfo = new DlpCommandDefinition(
            DlpOpcode.ReadSysInfo,
            new DlpArgumentDefinition[] { ReadSysInfoArgs.ReadSysInfoRequest },
            new DlpArgumentDefinition[] { ReadSysInfoArgs.ReadSysInfoResponse, ReadSysInfoArgs.ReadSysInfoDlpResponse });

        public static class ReadDbListArgs
        {
            public static readonly DlpArgumentDefinition<ReadDbListRequest> ReadDbListRequest = new DlpArgumentDefinition<ReadDbListRequest>();

            public static readonly DlpArgumentDefinition<ReadDbListResponse> ReadDbListResponse = new DlpArgumentDefinition<ReadDbListResponse>();
        }

        public static readonly DlpCommandDefinition ReadDbList = new DlpCommandDefinition(
            DlpOpcode.ReadDbList,
            new DlpArgumentDefinition[] { ReadDbListArgs.ReadDbListRequest },
            new DlpArgumentDefinition[] { ReadDbListArgs.ReadDbListResponse });

        public static class EndOfSyncArgs
        {
            public static readonly DlpArgumentDefinition<EndOfSyncRequest> EndOfSyncRequest = new DlpArgumentDefinition<EndOfSyncRequest>(false);
        }

        public static readonly DlpCommandDefinition EndOfSync = new DlpCommandDefinition(
            DlpOpcode.EndOfSync,
            new DlpArgumentDefinition[] { EndOfSyncArgs.EndOfSyncRequest },
            new DlpArgumentDefinition[] { });
    }
}
