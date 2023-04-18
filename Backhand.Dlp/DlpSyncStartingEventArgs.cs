using Backhand.Protocols.Dlp;
using System;

namespace Backhand.Dlp
{
    public class DlpSyncStartingEventArgs<TContext> : EventArgs
    {
        public DlpConnection Connection { get; }
        public TContext Context { get; }

        public DlpSyncStartingEventArgs(DlpConnection connection, TContext context)
        {
            Connection = connection;
            Context = context;
        }
    }
}
