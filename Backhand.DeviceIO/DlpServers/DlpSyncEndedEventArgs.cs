using System;

namespace Backhand.DeviceIO.DlpServers
{
    public class DlpSyncEndedEventArgs
    {
        public DlpContext Context { get; }
        public Exception? SyncException { get; }

        public DlpSyncEndedEventArgs(DlpContext context, Exception? syncException)
        {
            Context = context;
            SyncException = syncException;
        }
    }
}
