using Backhand.Common.Buffers;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;

namespace Backhand.Protocols.Slp.Internal
{
    internal static class SlpLogging
    {
        // Log messages
        private static readonly Action<ILogger, byte, byte, byte, byte, string, Exception> s_enqueueingPacket =
            LoggerMessage.Define<byte, byte, byte, byte, string>(
                LogLevel.Debug,
                new EventId(1, "Enqueueing packet"),
                "Equeueing packet; Dst: {destinationSocket}, Src: {sourceSocket}, Type: {packetType}, TxId: {transactionId}, Body: [{body}]"
            );

        private static readonly Action<ILogger, byte, byte, byte, byte, string, Exception> s_receivedPacket =
            LoggerMessage.Define<byte, byte, byte, byte, string>(
                LogLevel.Debug,
                new EventId(1, "Received packet"),
                "Received packet; Dst: {destinationSocket}, Src: {sourceSocket}, Type: {packetType}, TxId: {transactionId}, Body: [{body}]"
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

        // ILogger extension methods
        public static void EnqueueingPacket(this ILogger logger, SlpPacket packet)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_enqueueingPacket(logger, packet.DestinationSocket, packet.SourceSocket, packet.PacketType, packet.TransactionId, HexSerialization.GetHexString(packet.Data), default!);
        }

        public static void ReceivedPacket(this ILogger logger, SlpPacket packet)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_receivedPacket(logger, packet.DestinationSocket, packet.SourceSocket, packet.PacketType, packet.TransactionId, HexSerialization.GetHexString(packet.Data), default!);
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
    }
}
