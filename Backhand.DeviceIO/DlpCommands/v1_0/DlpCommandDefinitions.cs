using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0.Arguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0
{
    public static class DlpCommandDefinitions
    {
        public static readonly DlpCommandDefinition ReadUserInfo = new DlpCommandDefinition(
            DlpOpcode.ReadUserInfo,
            new DlpArgumentDefinition[] { },
            new DlpArgumentDefinition[] { DlpArgumentDefinitions.ReadUserInfoResponse });

        public static readonly DlpCommandDefinition ReadSysInfo = new DlpCommandDefinition(
            DlpOpcode.ReadSysInfo,
            new DlpArgumentDefinition[] { DlpArgumentDefinitions.ReadSysInfoRequest },
            new DlpArgumentDefinition[] { DlpArgumentDefinitions.ReadSysInfoResponse, DlpArgumentDefinitions.ReadSysInfoDlpVersionsResponse });

        public static readonly DlpCommandDefinition ReadDbList = new DlpCommandDefinition(
            DlpOpcode.ReadDbList,
            new DlpArgumentDefinition[] { DlpArgumentDefinitions.ReadDbListRequest },
            new DlpArgumentDefinition[] { DlpArgumentDefinitions.ReadDbListResponse });

        public static readonly DlpCommandDefinition OpenDb = new DlpCommandDefinition(
            DlpOpcode.OpenDb,
            new DlpArgumentDefinition[] { DlpArgumentDefinitions.OpenDbRequest },
            new DlpArgumentDefinition[] { DlpArgumentDefinitions.OpenDbResponse });

        public static readonly DlpCommandDefinition EndOfSync = new DlpCommandDefinition(
            DlpOpcode.EndOfSync,
            new DlpArgumentDefinition[] { DlpArgumentDefinitions.EndOfSyncRequest },
            new DlpArgumentDefinition[] { });

        public static readonly DlpCommandDefinition ReadRecordIdList = new DlpCommandDefinition(
            DlpOpcode.ReadRecordIdList,
            new DlpArgumentDefinition[] { DlpArgumentDefinitions.ReadRecordIdListRequest },
            new DlpArgumentDefinition[] { DlpArgumentDefinitions.ReadRecordIdListResponse });
    }
}
