using Backhand.Dlp.Commands.v1_1.Arguments;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_1
{
    public static class DlpCommands
    {
        /************************************/
        /*  ReadNextRecInCategory           */
        /************************************/
        public static class ReadNextRecInCategoryArguments
        {
            public static readonly DlpArgumentDefinition<ReadNextRecInCategoryRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadNextRecInCategoryResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition ReadNextRecInCategory = new()
        {
            Opcode = DlpOpcodes.ReadNextRecInCategory,
            RequestArguments = new[] { ReadNextRecInCategoryArguments.Request },
            ResponseArguments = new[] { ReadNextRecInCategoryArguments.Response }
        };
    }
}