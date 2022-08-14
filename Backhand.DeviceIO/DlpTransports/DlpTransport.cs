using System;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpTransports
{
    public abstract class DlpTransport
    {
        public event EventHandler<DlpPayloadTransmittedEventArgs>? ReceivedPayload;
        public event EventHandler<DlpPayloadTransmittedEventArgs>? SendingPayload;

        public abstract Task SendPayload(DlpPayload payload);

        protected void OnReceivedPayload(DlpPayloadTransmittedEventArgs e) => ReceivedPayload?.Invoke(this, e);
        protected void OnSendingPayload(DlpPayloadTransmittedEventArgs e) => SendingPayload?.Invoke(this, e);
    }
}
