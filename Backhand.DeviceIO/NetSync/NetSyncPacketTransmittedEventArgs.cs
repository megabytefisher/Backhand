namespace Backhand.DeviceIO.NetSync
{
    public class NetSyncPacketTransmittedEventArgs
    {
        public NetSyncPacket Packet { get; }

        public NetSyncPacketTransmittedEventArgs(NetSyncPacket packet)
        {
            Packet = packet;
        }
    }
}
