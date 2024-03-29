﻿using System.Buffers;

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
