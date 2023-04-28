using Backhand.Dlp.Commands.v1_2.Arguments;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_2
{
    public static class DlpCommands
    {
        /************************************/
        /*  ReadSysInfo                     */
        /************************************/
        public static class ReadSysInfoArguments
        {
            public static readonly DlpArgumentDefinition<ReadSysInfoRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadSysInfoSystemResponse> SystemResponse = new();
            public static readonly DlpArgumentDefinition<ReadSysInfoDlpResponse> DlpResponse = new();
        }
        
        public static readonly DlpCommandDefinition ReadSysInfo = new()
        {
            Opcode = v1_0.DlpOpcodes.ReadSysInfo,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ReadSysInfoArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadSysInfoArguments.SystemResponse,
                ReadSysInfoArguments.DlpResponse
            }
        };
        
        /************************************/
        /*  ReadDbList                      */
        /************************************/
        public static class ReadDbListArguments
        {
            public static readonly DlpArgumentDefinition<ReadDbListRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadDbListResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition ReadDbList = new()
        {
            Opcode = v1_0.DlpOpcodes.ReadDbList,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ReadDbListArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadDbListArguments.Response
            }
        };
    }
}
