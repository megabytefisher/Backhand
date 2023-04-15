using System;

namespace Backhand.Protocols.NetSync
{
    public class NetSyncException : Exception
    {
        public NetSyncException(string message) : base(message)
        {
        }
    }
}