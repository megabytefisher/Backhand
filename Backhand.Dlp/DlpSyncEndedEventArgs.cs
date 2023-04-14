using Backhand.Protocols.Dlp;
using System;

namespace Backhand.Dlp
{
    public class DlpSyncEndedEventArgs : EventArgs
    {
        public DlpConnection Connection { get; }
        public Exception? SyncException { get; }

        public DlpSyncEndedEventArgs(DlpConnection connection, Exception? syncException)
        {
            Connection = connection;
            SyncException = syncException;
        }
    }
}
