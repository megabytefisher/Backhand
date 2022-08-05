using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpTransports
{
    public class DlpPayload
    {
        public ReadOnlySequence<byte> Buffer { get; private init; }

        public DlpPayload(ReadOnlySequence<byte> buffer)
        {
            Buffer = buffer;
        }
    }
}
