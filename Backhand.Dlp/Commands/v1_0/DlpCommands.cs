using Backhand.Protocols.Dlp;
using Backhand.Dlp.Commands.v1_0.Arguments;

namespace Backhand.Dlp.Commands.v1_0
{
    public static class DlpCommands
    {
        public static class ReadUserInfoArguments
        {
            public static readonly DlpArgumentDefinition<ReadUserInfoResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadUserInfo = new DlpCommandDefinition(
            DlpOpcodes.ReadUserInfo,
            new DlpArgumentDefinition[] { },
            new DlpArgumentDefinition[] { ReadUserInfoArguments.Response }
        );


        public static class ReadSysInfoArguments
        {
            public static readonly DlpArgumentDefinition<ReadSysInfoRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadSysInfoSystemResponse> SystemResponse = new();
            public static readonly DlpArgumentDefinition<ReadSysInfoDlpResponse> DlpResponse = new();
        }

        public static readonly DlpCommandDefinition ReadSysInfo = new DlpCommandDefinition(
            DlpOpcodes.ReadSysInfo,
            new DlpArgumentDefinition[] { ReadSysInfoArguments.Request },
            new DlpArgumentDefinition[] { ReadSysInfoArguments.SystemResponse, ReadSysInfoArguments.DlpResponse }
        );


        public static class ReadDbListArguments
        {
            public static readonly DlpArgumentDefinition<ReadDbListRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadDbListResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadDbList = new DlpCommandDefinition(
            DlpOpcodes.ReadDbList,
            new DlpArgumentDefinition[] { ReadDbListArguments.Request },
            new DlpArgumentDefinition[] { ReadDbListArguments.Response }
        );


        public static class CreateDbArguments
        {
            public static readonly DlpArgumentDefinition<CreateDbRequest> Request = new();
            public static readonly DlpArgumentDefinition<CreateDbResponse> Response = new();
        }

        public static readonly DlpCommandDefinition CreateDb = new DlpCommandDefinition(
            DlpOpcodes.CreateDb,
            new DlpArgumentDefinition[] { CreateDbArguments.Request },
            new DlpArgumentDefinition[] { CreateDbArguments.Response }
        );


        public static class CloseDbArguments
        {
            public static readonly DlpArgumentDefinition<CloseDbRequest> Request = new();
        }

        public static readonly DlpCommandDefinition CloseDb = new DlpCommandDefinition(
            DlpOpcodes.CloseDb,
            new DlpArgumentDefinition[] { CloseDbArguments.Request },
            new DlpArgumentDefinition[] { }
        );


        public static readonly DlpCommandDefinition OpenConduit = new DlpCommandDefinition(
            DlpOpcodes.OpenConduit,
            new DlpArgumentDefinition[] { },
            new DlpArgumentDefinition[] { });


        public static class EndSyncArguments
        {
            public static readonly DlpArgumentDefinition<EndSyncRequest> Request = new();
        }

        public static readonly DlpCommandDefinition EndSync = new DlpCommandDefinition(
            DlpOpcodes.EndSync,
            new DlpArgumentDefinition[] { EndSyncArguments.Request },
            new DlpArgumentDefinition[] { }
        );
    }
}
