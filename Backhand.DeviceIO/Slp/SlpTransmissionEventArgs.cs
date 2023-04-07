namespace Backhand.DeviceIO.Slp
{
    public class SlpTransmissionEventArgs
    {
        public SlpPacket Packet { get; }

        public SlpTransmissionEventArgs(SlpPacket packet)
        {
            Packet = packet;
        }
    }
}
