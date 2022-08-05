using Backhand.DeviceIO.NetSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpTransports
{
    public class NetSyncDlpTransport : DlpTransport, IDisposable
    {
        private NetSyncDevice _device;

        private byte _transactionId = 0x04 - 1;

        public NetSyncDlpTransport(NetSyncDevice device)
        {
            _device = device;

            _device.ReceivedPacket += device_ReceivedPacket;
        }

        public void Dispose()
        {
            _device.ReceivedPacket -= device_ReceivedPacket;
        }

        public override Task SendPayload(DlpPayload payload)
        {
            OnSendingPayload(new DlpPayloadTransmittedEventArgs(payload));
            BumpTransactionId();
            _device.SendPacket(new NetSyncPacket(_transactionId, payload.Buffer));
            return Task.CompletedTask;
        }

        private void device_ReceivedPacket(object? sender, NetSyncPacketTransmittedEventArgs e)
        {
            if (e.Packet.TransactionId == _transactionId)
                OnReceivedPayload(new DlpPayloadTransmittedEventArgs(new DlpPayload(e.Packet.Data)));
        }

        private void BumpTransactionId()
        {
            _transactionId = (byte)(_transactionId + 1);
            if (_transactionId == 0xff ||
                _transactionId == 0x00 ||
                _transactionId == 0x01)
            {
                _transactionId = 0x02;
            }    
        }
    }
}
