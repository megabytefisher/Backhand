using System;
using System.Buffers;
using System.Linq;
using System.Text;
using Backhand.Common.Buffers;
using Microsoft.Extensions.Logging;

namespace Backhand.Protocols.Dlp.Internal
{
    internal static class DlpLogging
    {
        private static readonly Action<ILogger, byte, string, Exception> s_executingTransaction =
            LoggerMessage.Define<byte, string>(
                LogLevel.Debug,
                new EventId(1, "Executing transaction"),
                "Executing transaction; Opcode: {opcode}, Arguments: {arguments}"
            );

        private static readonly Action<ILogger, byte, string, Exception> s_receivedTransactionResponse =
            LoggerMessage.Define<byte, string>(
                LogLevel.Debug,
                new EventId(1, "Received transaction response"),
                "Received transaction response; Opcode: {opcode}, Arguments: {arguments}"
            );

        public static void ExecutingTransaction(this ILogger logger, DlpCommandDefinition commandDefinition, DlpArgumentMap requestArguments)
        {
            string argsString = string.Join(", ", commandDefinition.RequestArguments.Select(d => $"{d.Type.Name}={requestArguments.GetValue(d)?.ToString() ?? "null"}"));

            s_executingTransaction(logger, commandDefinition.Opcode, argsString, default!);
        }

        public static void ReceivedTransactionResponse(this ILogger logger, DlpCommandDefinition commandDefinition, DlpArgumentMap responseArguments)
        {
            string argsString = string.Join(", ", commandDefinition.ResponseArguments.Select(d => $"{d.Type.Name}={responseArguments.GetValue(d)?.ToString() ?? "null"}"));

            s_receivedTransactionResponse(logger, commandDefinition.Opcode, argsString, default!);
        }
    }
}