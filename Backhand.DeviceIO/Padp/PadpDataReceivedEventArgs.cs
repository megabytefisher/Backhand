using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Padp
{
    public class PadpDataReceivedEventArgs
    {
        public ReadOnlySequence<byte> Data { get; private init; }

        public PadpDataReceivedEventArgs(ReadOnlySequence<byte> data)
        {
            Data = data;
        }
    }
}
