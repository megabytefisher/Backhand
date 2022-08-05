﻿using Backhand.DeviceIO.Padp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpTransports
{
    public class PadpDlpTransport : DlpTransport, IDisposable
    {
        private PadpConnection _padp;

        public PadpDlpTransport(PadpConnection padp)
        {
            _padp = padp;

            _padp.ReceivedData += padp_ReceivedData;
        }

        public void Dispose()
        {
            _padp.ReceivedData -= padp_ReceivedData;
        }

        public override async Task SendPayload(DlpPayload payload)
        {
            OnSendingPayload(new DlpPayloadTransmittedEventArgs(payload));
            _padp.BumpTransactionId();
            await _padp.SendData(payload.Buffer);
        }

        private void padp_ReceivedData(object? sender, PadpDataReceivedEventArgs e)
        {
            OnReceivedPayload(new DlpPayloadTransmittedEventArgs(new DlpPayload(e.Data)));
        }
    }
}
