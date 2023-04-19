using System;
using Microsoft.Extensions.Logging;
using static Backhand.Protocols.Cmp.CmpConnection;

namespace Backhand.Protocols.Cmp.Internal
{
    internal static class CmpLogging
    {
        private static readonly Action<ILogger, Exception> s_listeningForWakeUp =
            LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(1, "Listening for wake up"),
                "Listening for wake up"
            );

        private static readonly Action<ILogger, Exception> s_wakeUpReceived =
            LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(1, "Wake up received"),
                "Wake up received"
            );

        private static readonly Action<ILogger, CmpPacketType, CmpInitPacketFlags, byte, byte, uint, Exception> s_sendingInitPacket =
            LoggerMessage.Define<CmpPacketType, CmpInitPacketFlags, byte, byte, uint>(
                LogLevel.Debug,
                new EventId(1, "Sending init packet"),
                "Sending init packet; Type: {type}, Flags: {flags}, MajorVersion: {majorVersion}, MinorVersion: {minorVersion}, NewBaudRate: {newBaudRate}"
            );

        public static void ListeningForWakeUp(this ILogger logger)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_listeningForWakeUp(logger, default!);
        }

        public static void WakeUpReceived(this ILogger logger)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_wakeUpReceived(logger, default!);
        }

        public static void SendingInitPacket(this ILogger logger, CmpInitPacket packet)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            s_sendingInitPacket(logger, packet.Type, packet.Flags, packet.MajorVersion, packet.MinorVersion, packet.NewBaudRate, default!);
        }
    }
}