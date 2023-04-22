using Backhand.Protocols.Dlp;
using Microsoft.Extensions.Logging;
using System;

namespace Backhand.Dlp
{
    public static class DlpServerLogging
    {
        public const string DlpServerCategory = "dlpserver";
        public const string SlpInterfaceCategory = "slp";
        public const string PadpConnectionCategory = "padp";
        public const string CmpConnectionCategory = "cmp";
        public const string NetSyncInterfaceCategory = "netsync";
        public const string NetSyncConnectionCategory = "netsync";
        public const string DlpConnectionCategory = "dlp";

        public static readonly EventId DlpServerStartingId = new(1, "Server Starting");
        public static readonly EventId DlpServerConnectionOpenedId = new(2, "Connection Opened");
        public static readonly EventId DlpServerStartingSyncId = new(3, "Starting Sync");
        public static readonly EventId DlpServerSyncEndedId = new(4, "Sync Ended");
        public static readonly EventId DlpServerConnectionClosedId = new(5, "Connection Closed");
        public static readonly EventId DlpServerStoppedId = new(6, "Server Stopped");

        private static readonly Action<ILogger, string, Exception?> s_serverStarting =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                DlpServerStartingId,
                "Server starting; Server: {server}"
            );

        private static readonly Action<ILogger, string, string, Exception?> s_newConnection =
            LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                DlpServerConnectionOpenedId,
                "Connection opened; Server: {server}, Connection: {connection}"
            );

        private static readonly Action<ILogger, string, string, Exception?> s_startingSync =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                DlpServerStartingSyncId,
                "Starting sync; Server: {server}, Connection: {connection}"
            );

        private static readonly Action<ILogger, string, string, Exception?> s_syncEnded =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                DlpServerSyncEndedId,
                "Sync ended; Server: {server}, Connection: {connection}"
            );
        
        private static readonly Action<ILogger, string, string, string, Exception> s_syncEndedWithError =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Error,
                DlpServerSyncEndedId,
                "Sync ended; Server: {server}, Connection: {connection}, Exception: {exception}"
            );

        private static readonly Action<ILogger, string, string, Exception?> s_connectionClosed =
            LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                DlpServerConnectionClosedId,
                "Connection closed; Server: {server}, Connection: {connection}"
            );

        private static readonly Action<ILogger, string, string, string, Exception> s_connectionClosedWithError =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Error,
                DlpServerConnectionClosedId,
                "Connection closed; Server: {server}, Connection: {connection}, Exception: {exception}"
            );
        
        private static readonly Action<ILogger, string, Exception?> s_serverStopped =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                DlpServerStoppedId,
                "Server stopped; Server: {server}"
            );
        
        private static readonly Action<ILogger, string, Exception?> s_serverStoppedDueToCancellation =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                DlpServerStoppedId,
                "Server stopped due to cancellation request; Server: {server}"
            );
        
        private static readonly Action<ILogger, string, string, Exception> s_serverStoppedWithError =
            LoggerMessage.Define<string, string>(
                LogLevel.Error,
                DlpServerStoppedId,
                "Server stopped; Server: {server}, Exception: {exception}"
            );

        internal static void ServerStarting(this ILogger logger, DlpServer server)
        {
            s_serverStarting(logger, server.ToString()!, default);
        }

        internal static void ConnectionOpened(this ILogger logger, DlpServer server, string connectionName)
        {
            s_newConnection(logger, server.ToString()!, connectionName, default);
        }

        internal static void StartingSync(this ILogger logger, DlpServer server, DlpConnection connection)
        {
            s_startingSync(logger, server.ToString()!, connection.ToString()!, default);
        }
        
        internal static void SyncEnded(this ILogger logger, DlpServer server, DlpConnection connection, Exception? exception = null)
        {
            if (exception != null)
            {
                s_syncEndedWithError(logger, server.ToString()!, connection.ToString()!, exception.GetType().Name, exception);
            }
            else
            {
                s_syncEnded(logger, server.ToString()!, connection.ToString()!, default);
            }
        }

        internal static void ConnectionClosed(this ILogger logger, DlpServer server, string connectionName, Exception? exception = null)
        {
            if (exception != null)
            {
                s_connectionClosedWithError(logger, server.ToString()!, connectionName, exception.GetType().Name, exception);
            }
            else
            {
                s_connectionClosed(logger, server.ToString()!, connectionName, default);
            }
        }
        
        internal static void ServerStopped(this ILogger logger, DlpServer server, Exception? exception = null)
        {
            switch (exception)
            {
                case null:
                    s_serverStopped(logger, server.ToString()!, default);
                    break;
                case OperationCanceledException:
                    s_serverStoppedDueToCancellation(logger, server.ToString()!, default);
                    break;
                default:
                    s_serverStoppedWithError(logger, server.ToString()!, exception.GetType().Name, exception);
                    break;
            }
        }
    }
}
