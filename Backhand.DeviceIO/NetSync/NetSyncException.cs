using System;

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
