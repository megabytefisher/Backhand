using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Protocols.NetSync
{
    public class NetSyncPacket
    {
        public byte TransactionId { get; }
        public ReadOnlySequence<byte> Data { get; }

        public NetSyncPacket(byte transactionId, ReadOnlySequence<byte> data)
        {
            TransactionId = transactionId;
            Data = data;
        }
    }
}
