using System.Buffers;

namespace Backhand.DeviceIO.Padp
{
    public class PadpDataReceivedEventArgs
    {
        public ReadOnlySequence<byte> Data { get; }

        public PadpDataReceivedEventArgs(ReadOnlySequence<byte> data)
        {
            Data = data;
        }
    }
}
