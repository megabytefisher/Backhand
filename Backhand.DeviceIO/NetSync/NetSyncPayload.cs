using System.Buffers;

namespace Backhand.DeviceIO.NetSync
{
    public class NetSyncPayload
    {
        public ReadOnlySequence<byte> Buffer { get; }

        public NetSyncPayload(ReadOnlySequence<byte> buffer)
        {
            Buffer = buffer;
        }
    }
}