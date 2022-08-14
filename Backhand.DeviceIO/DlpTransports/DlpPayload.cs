using System.Buffers;

namespace Backhand.DeviceIO.DlpTransports
{
    public class DlpPayload
    {
        public ReadOnlySequence<byte> Buffer { get; }

        public DlpPayload(ReadOnlySequence<byte> buffer)
        {
            Buffer = buffer;
        }
    }
}
