using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public class DlpSyncEndedEventArgs
    {
        public DlpContext Context { get; private init; }
        public Exception? SyncException { get; private init; }

        public DlpSyncEndedEventArgs(DlpContext context, Exception? syncException)
        {
            Context = context;
            SyncException = syncException;
        }
    }
}
