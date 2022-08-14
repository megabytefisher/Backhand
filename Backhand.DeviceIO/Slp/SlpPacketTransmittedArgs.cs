using System;

namespace Backhand.DeviceIO.Slp
{
    public class SlpPacketTransmittedArgs : EventArgs
    {
        public SlpPacket Packet { get; }

        public SlpPacketTransmittedArgs(SlpPacket packet)
        {
            Packet = packet;
        }
    }
}
