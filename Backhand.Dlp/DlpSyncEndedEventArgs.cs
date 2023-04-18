using Backhand.Protocols.Dlp;
using System;

namespace Backhand.Dlp
{
    public class DlpSyncEndedEventArgs<TContext> : EventArgs
    {
        public DlpConnection Connection { get; }
        public TContext Context { get; }
        public Exception? SyncException { get; }

        public DlpSyncEndedEventArgs(DlpConnection connection, TContext context, Exception? syncException)
        {
            Connection = connection;
            Context = context;
            SyncException = syncException;
        }
    }
}
