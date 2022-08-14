namespace Backhand.DeviceIO.DlpTransports
{
    public class DlpPayloadTransmittedEventArgs
    {
        public DlpPayload Payload { get; }

        public DlpPayloadTransmittedEventArgs(DlpPayload payload)
        {
            Payload = payload;
        }
    }
}
