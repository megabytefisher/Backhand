using System;
using System.Buffers;
using Backhand.Common.Buffers;
using Microsoft.Extensions.Logging;

namespace Backhand.Protocols.NetSync.Internal
{
    internal static class NetSyncLogging
    {
        private static readonly Action<ILogger, byte, string, Exception> s_enqueueingDataPacket =
            LoggerMessage.Define<byte, string>(
                LogLevel.Debug,
                new EventId(1, "Enqueueing data packet"),
                "Enqueueing data packet; TransactionId: {transactionId}, Data: {data}"
            );

        private static readonly Action<ILogger, byte, string, Exception> s_receivedDataPacket =
            LoggerMessage.Define<byte, string>(
                LogLevel.Debug,
                new EventId(1, "Received data packet"),
                "Received data packet; TransactionId: {transactionId}, Data: {data}"
            );

        private static readonly Action<ILogger, int, string, Exception> s_writingBytes =
            LoggerMessage.Define<int, string>(
                LogLevel.Trace,
                new EventId(1, "Writing bytes"),
                "Writing {length} bytes to pipe; Content: [{bytes}]"
            );

        private static readonly Action<ILogger, string, Exception> s_readBytes =
            LoggerMessage.Define<string>(
                LogLevel.Trace,
                new EventId(1, "Read bytes"),
                "Read from pipe; Buffer: [{bytes}]"
            );

        private static readonly Action<ILogger, byte, string, Exception> s_sendingPayload =
            LoggerMessage.Define<byte, string>(
                LogLevel.Debug,
                new EventId(1, "Sending payload"),
                "Sending payload; TransactionId: {transactionId}, Body: [{body}]"
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
                "Received payload; TransactionId: {transactionId}, Body: [{body}]"
            );

        public static void EnqueueingPacket(this ILogger logger, NetSyncPacket packet)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_enqueueingDataPacket(logger, packet.TransactionId, HexSerialization.GetHexString(packet.Data), default!);
        }

        public static void ReceivedPacket(this ILogger logger, NetSyncPacket packet)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_receivedDataPacket(logger, packet.TransactionId, HexSerialization.GetHexString(packet.Data), default!);
        }

        public static void WritingBytes(this ILogger logger, ReadOnlySpan<byte> bytes)
        {
            if (!logger.IsEnabled(LogLevel.Trace))
            {
                return;
            }

            s_writingBytes(logger, bytes.Length, HexSerialization.GetHexString(bytes), default!);
        }

        public static void ReadBytes(this ILogger logger, ReadOnlySequence<byte> bytes)
        {
            if (!logger.IsEnabled(LogLevel.Trace))
            {
                return;
            }

            s_readBytes(logger, HexSerialization.GetHexString(bytes), default!);
        }

        public static void SendingPayload(this ILogger logger, NetSyncPacket packet)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_sendingPayload(logger, packet.TransactionId, HexSerialization.GetHexString(packet.Data), default!);
        }

        public static void WaitingForPayload(this ILogger logger, byte transactionId)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_waitingForPayload(logger, transactionId, default!);
        }

        public static void ReceivedPayload(this ILogger logger, NetSyncPacket packet)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_receivedPayload(logger, packet.TransactionId, HexSerialization.GetHexString(packet.Data), default!);
        }
    }
}