using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpTransports
{
    public class DlpPayloadTransmittedEventArgs
    {
        public DlpPayload Payload { get; private init; }

        public DlpPayloadTransmittedEventArgs(DlpPayload payload)
        {
            Payload = payload;
        }
    }
}
