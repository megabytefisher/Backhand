using System;

namespace Backhand.DeviceIO.DlpServers
{
    public class DlpSyncStartingEventArgs : EventArgs
    {
        public DlpClientContext ClientContext { get; }

        public DlpSyncStartingEventArgs(DlpClientContext clientContext)
        {
            ClientContext = clientContext;
        }
    }
}
