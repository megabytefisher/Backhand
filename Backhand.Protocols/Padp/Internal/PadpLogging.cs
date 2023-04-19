using System;
using System.Buffers;
using Backhand.Common.Buffers;
using Microsoft.Extensions.Logging;

namespace Backhand.Protocols.Padp.Internal
{
    internal static class PadpLogging
    {
        private static readonly Action<ILogger, byte, string, Exception> s_sendingPayload =
            LoggerMessage.Define<byte, string>(
                LogLevel.Debug,
                new EventId(1, "Sending payload"),
                "Sending payload; TransactionId: {type}, Body: [{body}]"
            );

        private static readonly Action<ILogger, byte, Exception> s_waitingForPayload =
            LoggerMessage.Define<byte>(
                LogLevel.Debug,
                new EventId(1, "Waiting for payload"),
                "Waiting for payload; TransactionId: {transactionId}"
            );

        private static readonly Action<ILogger, byte, string, Exception> s_receivedPayload =
            LoggerMessage.Define<byte, string>(
                LogLevel.Debug,
                new EventId(1, "Received payload"),
                "Received payload; TransactionId: {type}, Body: [{body}]"
            );

        private static readonly Action<ILogger, byte, PadpConnection.PadpFragmentType, PadpConnection.PadpFragmentFlags, uint, string, Exception> s_sendingFragment =
            LoggerMessage.Define<byte, PadpConnection.PadpFragmentType, PadpConnection.PadpFragmentFlags, uint, string>(
                LogLevel.Debug,
                new EventId(1, "Sending fragment"),
                "Sending fragment; TransactionId: {transactionId}, Type: {type}, Flags: {flags}, SizeOrOffset: {sequence}, Body: [{body}]"
            );

        private static readonly Action<ILogger, byte, PadpConnection.PadpFragmentType, PadpConnection.PadpFragmentFlags, uint, string, Exception> s_receivedFragment =
            LoggerMessage.Define<byte, PadpConnection.PadpFragmentType, PadpConnection.PadpFragmentFlags, uint, string>(
                LogLevel.Debug,
                new EventId(1, "Received fragment"),
                "Received fragment; TransactionId: {transactionId}, Type: {type}, Flags: {flags}, SizeOrOffset: {sequence}, Body: [{body}]"
            );

        public static void SendingPayload(this ILogger logger, byte transactionId, ReadOnlySequence<byte> body)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_sendingPayload(logger, transactionId, HexSerialization.GetHexString(body), default!);
        }

        public static void WaitingForPayload(this ILogger logger, byte transactionId)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_waitingForPayload(logger, transactionId, default!);
        }

        public static void ReceivedPayload(this ILogger logger, byte transactionId, ReadOnlySequence<byte> body)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_receivedPayload(logger, transactionId, HexSerialization.GetHexString(body), default!);
        }

        public static void SendingFragment(this ILogger logger, byte transactionId, PadpConnection.PadpFragmentType type, PadpConnection.PadpFragmentFlags flags, uint sequence, ReadOnlySequence<byte> body)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_sendingFragment(logger, transactionId, type, flags, sequence, HexSerialization.GetHexString(body), default!);
        }

        public static void ReceivedFragment(this ILogger logger, byte transactionId, PadpConnection.PadpFragmentType type, PadpConnection.PadpFragmentFlags flags, uint sequence, ReadOnlySequence<byte> body)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_receivedFragment(logger, transactionId, type, flags, sequence, HexSerialization.GetHexString(body), default!);
        }
    }
}