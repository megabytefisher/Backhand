using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.NetSync
{
    public class NetSyncPacket
    {
        public byte TransactionId { get; private init; }
        public ReadOnlySequence<byte> Data { get; private init; }

        public NetSyncPacket(byte transactionId, ReadOnlySequence<byte> data)
        {
            TransactionId = transactionId;
            Data = data;
        }
    }
}
