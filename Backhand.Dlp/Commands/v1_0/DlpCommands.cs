using Backhand.Protocols.Dlp;
using Backhand.Dlp.Commands.v1_0.Arguments;
using System;

namespace Backhand.Dlp.Commands.v1_0
{
    public static class DlpCommands
    {
        public static class ReadUserInfoArguments
        {
            public static readonly DlpArgumentDefinition<ReadUserInfoResponse> Result = new();
        }

        public static readonly DlpCommandDefinition ReadUserInfo = new DlpCommandDefinition(
            DlpOpcode.ReadUserInfo,
            Array.Empty<DlpArgumentDefinition>(),
            new[] { ReadUserInfoArguments.Result });

        public static class EndOfSyncArguments
        {
            public static readonly DlpArgumentDefinition<EndOfSyncRequest> Request = new();
        }

        public static readonly DlpCommandDefinition EndOfSync = new DlpCommandDefinition(
            DlpOpcode.EndOfSync,
            new[] { EndOfSyncArguments.Request },
            Array.Empty<DlpArgumentDefinition>());
    }
}
