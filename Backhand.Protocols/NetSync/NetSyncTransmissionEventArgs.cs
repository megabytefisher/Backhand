namespace Backhand.Protocols.NetSync
{
    public class NetSyncTransmissionEventArgs
    {
        public NetSyncPacket Packet { get; }
        
        public NetSyncTransmissionEventArgs(NetSyncPacket packet)
        {
            Packet = packet;
        }
    }
}
