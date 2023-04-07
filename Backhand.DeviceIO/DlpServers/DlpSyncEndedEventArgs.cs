using System;

namespace Backhand.DeviceIO.DlpServers
{
    public class DlpSyncEndedEventArgs
    {
        public DlpClientContext ClientContext { get; }
        public Exception? SyncException { get; }

        public DlpSyncEndedEventArgs(DlpClientContext clientContext, Exception? syncException)
        {
            ClientContext = clientContext;
            SyncException = syncException;
        }
    }
}
