using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Backhand.Protocols.Dlp
{
    public static class DlpLogging
    {
        public static readonly EventId DlpExecutingTransactionId = new(1, "Executing Transaction");
        public static readonly EventId DlpReceivedTransactionResponseId = new(2, "Received Transaction Response");

        private static readonly Action<ILogger, byte, string, Exception> s_executingTransaction =
            LoggerMessage.Define<byte, string>(
                LogLevel.Debug,
                DlpExecutingTransactionId,
                "Executing transaction; Opcode: {opcode}, Arguments: {arguments}"
            );

        private static readonly Action<ILogger, byte, string, Exception> s_receivedTransactionResponse =
            LoggerMessage.Define<byte, string>(
                LogLevel.Debug,
                DlpReceivedTransactionResponseId,
                "Received transaction response; Opcode: {opcode}, Arguments: {arguments}"
            );

        internal static void ExecutingTransaction(this ILogger logger, DlpCommandDefinition commandDefinition, DlpArgumentMap requestArguments)
        {
            string argsString = string.Join(", ", commandDefinition.RequestArguments.Select(d => $"{d.Type.Name}={requestArguments.GetValue(d)?.ToString() ?? "null"}"));

            s_executingTransaction(logger, commandDefinition.Opcode, argsString, default!);
        }

        internal static void ReceivedTransactionResponse(this ILogger logger, DlpCommandDefinition commandDefinition, DlpArgumentMap responseArguments)
        {
            string argsString = string.Join(", ", commandDefinition.ResponseArguments.Select(d => $"{d.Type.Name}={responseArguments.GetValue(d)?.ToString() ?? "null"}"));

            s_receivedTransactionResponse(logger, commandDefinition.Opcode, argsString, default!);
        }
    }
}