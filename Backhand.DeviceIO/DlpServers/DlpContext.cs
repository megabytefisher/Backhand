using Backhand.DeviceIO.Dlp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpServers
{
    public class DlpContext
    {
        public DlpConnection Connection { get; private init; }

        public DlpContext(DlpConnection connection)
        {
            Connection = connection;
        }
    }
}
