using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public class DlpSyncStartingEventArgs : EventArgs
    {
        public DlpContext Context { get; private init; }

        public DlpSyncStartingEventArgs(DlpContext context)
        {
            Context = context;
        }
    }
}
