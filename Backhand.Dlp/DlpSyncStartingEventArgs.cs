using Backhand.Protocols.Dlp;
using System;

namespace Backhand.Dlp
{
    public class DlpSyncStartingEventArgs : EventArgs
    {
        public DlpConnection Connection { get; }

        public DlpSyncStartingEventArgs(DlpConnection connection)
        {
            Connection = connection;
        }
    }
}
