using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Protocols.NetSync.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backhand.Protocols.NetSync
{
    public class NetSyncConnection : IDlpTransport
    {
        private readonly NetSyncInterface _interface;
        private byte _transactionId = 0xff;

        private readonly ILogger _logger;
        
        private static readonly byte[] NetSyncHandshakeWakeup = {
            0x90, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00,
            0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] NetSyncHandshakeRequest1 = {
            0x12, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00,
            0x00, 0x24, 0xff, 0xff, 0xff, 0xff, 0x3c, 0x00, 0x3c, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0xa8, 0x01, 0x21, 0x04, 0x27,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00,
        };

        private static readonly byte[] NetSyncHandshakeResponse1 = {
            0x92, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00,
            0x00, 0x24, 0xff, 0xff, 0xff, 0xff, 0x00, 0x3c, 0x00, 0x3c, 0x40, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xc0, 0xa8, 0xa5, 0x1e, 0x04, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00,
        };

        private static readonly byte[] NetSyncHandshakeRequest2 = {
            0x13, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00,
            0x00, 0x20, 0xff, 0xff, 0xff, 0xff, 0x00, 0x3c, 0x00, 0x3c, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] NetSyncHandshakeResponse2 = {
            0x93, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        public NetSyncConnection(NetSyncInterface netSyncInterface, ILogger? logger = null)
        {
            _interface = netSyncInterface;
            _logger = logger ?? NullLogger.Instance;
        }

        public async Task ExecuteTransactionAsync(ReadOnlySequence<byte> requestData, Action<ReadOnlySequence<byte>> responseCallback, CancellationToken cancellationToken = default)
        {
            byte transactionId = GetNewTransactionId();
            Task receivePayloadTask = ReceivePayloadAsync(transactionId, responseCallback, cancellationToken);
            SendPayload(transactionId, requestData);
            await receivePayloadTask;
        }

        public async Task DoHandshakeAsync(CancellationToken cancellationToken)
        {
            _transactionId = 0xff;

            // Wait for wakeup packet
            await ReceivePayloadAsync(
                _transactionId,
                data =>
                {
                    if (data.Length != NetSyncHandshakeWakeup.Length)
                    {
                        throw new NetSyncException("Received packet with wrong length");
                    }
                },
                cancellationToken
            );

            // First hanshake transaction
            await ExecuteTransactionAsync(
                new ReadOnlySequence<byte>(NetSyncHandshakeRequest1),
                data =>
                {
                    if (data.Length != NetSyncHandshakeResponse1.Length)
                    {
                        throw new NetSyncException("Received packet with wrong length");
                    }
                },
                cancellationToken
            );

            // Second handshake transaction
            await ExecuteTransactionAsync(
                new ReadOnlySequence<byte>(NetSyncHandshakeRequest2),
                data =>
                {
                    if (data.Length != NetSyncHandshakeResponse2.Length)
                    {
                        throw new NetSyncException("Received packet with wrong length");
                    }
                },
                cancellationToken
            );
        }

        private byte GetNewTransactionId()
        {
            if (++_transactionId is 0xff or 0x00 or 0x01)
            {
                _transactionId = 0x02;
            }
            return _transactionId;
        }

        private async Task ReceivePayloadAsync(byte transactionId, Action<ReadOnlySequence<byte>> callback, CancellationToken cancellationToken)
        {
            _logger.WaitingForPayload(transactionId);

            TaskCompletionSource receiveTcs = new();

            void OnNetSyncPacketReceived(object? sender, NetSyncTransmissionEventArgs e)
            {
                try
                {
                    if (e.Packet.TransactionId != transactionId)
                    {
                        throw new NetSyncException("Received packet with wrong transaction ID");
                    }

                    _logger.ReceivedPayload(e.Packet);
                    callback(e.Packet.Data);
                    receiveTcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    receiveTcs.TrySetException(ex);
                }
            }

            _interface.PacketReceived += OnNetSyncPacketReceived;
            try
            {
                await using (cancellationToken.Register(() => receiveTcs.TrySetCanceled()))
                {
                    await receiveTcs.Task.ConfigureAwait(false);
                }
            }
            finally 
            {
                _interface.PacketReceived -= OnNetSyncPacketReceived;
            }
        }

        private void SendPayload(byte transactionId, ReadOnlySequence<byte> buffer)
        {
            NetSyncPacket payloadPacket = new NetSyncPacket(transactionId, buffer);
            _logger.SendingPayload(payloadPacket);
            _interface.EnqueuePacket(payloadPacket);
        }
    }
}