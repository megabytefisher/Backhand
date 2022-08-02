using Backhand.DeviceIO.Dlp.Arguments.Response;
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
            public static readonly DlpArgumentDefinition<DeviceUserInfo> ResponseUserInfo = new DlpArgumentDefinition<DeviceUserInfo>(false);
        }

        public static readonly DlpCommandDefinition ReadUserInfo = new DlpCommandDefinition(
            DlpOpcode.ReadUserInfo,
            new DlpArgumentDefinition[] { },
            new DlpArgumentDefinition[] { ReadUserInfoArgs.ResponseUserInfo });

        /*public static class ReadSysInfoArgs
        {
            public static readonly DlpArgumentDefinition<HostSysInfo> RequestHostSysInfo = new DlpArgumentDefinition<HostSysInfo>(false);

            public static readonly DlpArgumentDefinition<DeviceSysInfo> ResponseDeviceSysInfo = new DlpArgumentDefinition<DeviceSysInfo>(false);
            public static readonly DlpArgumentDefinition<DeviceSysVersionInfo> ResponseDeviceSysVersionInfo = new DlpArgumentDefinition<DeviceSysVersionInfo>(false);
        }

        public static readonly DlpCommandDefinition ReadSysInfo = new DlpCommandDefinition(
            DlpOpcode.ReadSysInfo,
            new DlpArgumentDefinition[] { ReadSysInfoArgs.RequestHostSysInfo },
            new DlpArgumentDefinition[] { ReadSysInfoArgs.ResponseDeviceSysInfo, ReadSysInfoArgs.ResponseDeviceSysVersionInfo });

        public static class EndOfSyncArgs
        {
            public static readonly DlpArgumentDefinition<EndOfSyncInfo> RequestEndOfSyncInfo = new DlpArgumentDefinition<EndOfSyncInfo>(false);
        }

        public static readonly DlpCommandDefinition EndOfSync = new DlpCommandDefinition(
            DlpOpcode.EndOfSync,
            new DlpArgumentDefinition[] { EndOfSyncArgs.RequestEndOfSyncInfo },
            new DlpArgumentDefinition[] { });*/
    }
}
