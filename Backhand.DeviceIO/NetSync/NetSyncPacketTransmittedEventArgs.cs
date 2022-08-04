using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.NetSync
{
    public class NetSyncPacketTransmittedEventArgs
    {
        public NetSyncPacket Packet { get; private init; }

        public NetSyncPacketTransmittedEventArgs(NetSyncPacket packet)
        {
            Packet = packet;
        }
    }
}
