using System.Buffers;

namespace Backhand.DeviceIO.Padp
{
    public class PadpPayload
    {
        public ReadOnlySequence<byte> Buffer { get; }

        public PadpPayload(ReadOnlySequence<byte> buffer)
        {
            Buffer = buffer;
        }
    }
}