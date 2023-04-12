using Backhand.Common.Buffers;
using Microsoft.Extensions.Logging;
using System;

namespace Backhand.Protocols.Slp
{
    internal static class SlpLogging
    {
        // Log messages
        private static readonly Action<ILogger, byte, byte, byte, byte, string, Exception> s_enqueueingPacket = LoggerMessage.Define<byte, byte, byte, byte, string>(
            LogLevel.Debug,
            new EventId(1, "Enqueueing packet"),
            "Equeueing packet; Dst: {destinationSocket}, Src: {sourceSocket}, Type: {packetType}, TxId: {transactionId}, Body: [{body}]");

        private static readonly Action<ILogger, byte, byte, byte, byte, string, Exception> s_receivedPacket = LoggerMessage.Define<byte, byte, byte, byte, string>(
            LogLevel.Debug,
            new EventId(1, "Received packet"),
            "Received packet; Dst: {destinationSocket}, Src: {sourceSocket}, Type: {packetType}, TxId: {transactionId}, Body: [{body}]");

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
    }
}
