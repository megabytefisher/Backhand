using System;

namespace Backhand.DeviceIO.DlpServers
{
    public class DlpSyncStartingEventArgs : EventArgs
    {
        public DlpContext Context { get; }

        public DlpSyncStartingEventArgs(DlpContext context)
        {
            Context = context;
        }
    }
}
