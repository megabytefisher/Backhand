using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.NetSync
{
    public class NetSyncException : Exception
    {
        public NetSyncException(string message)
            : base(message)
        {
        }
    }
}
