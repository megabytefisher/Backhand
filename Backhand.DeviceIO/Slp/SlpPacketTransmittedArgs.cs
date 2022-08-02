using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Slp
{
    public class SlpPacketTransmittedArgs : EventArgs
    {
        public SlpPacket Packet { get; private init; }

        public SlpPacketTransmittedArgs(SlpPacket packet)
        {
            Packet = packet;
        }
    }
}
