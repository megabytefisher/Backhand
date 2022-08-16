using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.NetSync
{
    public class NetSyncConnection
    {
        private readonly NetSyncDevice _device;
        private byte _transactionId = 0xff;
        
        private static readonly byte[] NetSyncHandshakeWakeup = {
            0x90, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00,
            0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] NetSyncHandshakeRequest1 = {
            0x12, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00,
            0x24, 0xff, 0xff, 0xff, 0xff, 0x3c, 0x00, 0x3c, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0xc0, 0xa8, 0x01, 0x21, 0x04, 0x27, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] NetSyncHandshakeResponse1 = {
            0x92, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00,
            0x24, 0xff, 0xff, 0xff, 0xff, 0x00, 0x3c, 0x00, 0x3c, 0x40, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0xc0, 0xa8, 0xa5, 0x1e, 0x04, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] NetSyncHandshakeRequest2 = {
            0x13, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00,
            0x20, 0xff, 0xff, 0xff, 0xff, 0x00, 0x3c, 0x00, 0x3c, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] NetSyncHandshakeResponse2 = {
            0x93, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        public NetSyncConnection(NetSyncDevice device)
        {
            _device = device;
        }
        
        public void BumpTransactionId()
        {
            _transactionId++;
            if (_transactionId is 0xff or 0x00 or 0x01)
            {
                _transactionId = 0x02;
            }
        }

        public async Task ExecuteTransactionAsync(NetSyncPayload sendPayload,
            Action<NetSyncPayload> handleReceivePayloadAction, CancellationToken cancellationToken = default)
        {
            BumpTransactionId();
            Task receivePayloadTask = ReceivePayloadAsync(handleReceivePayloadAction, cancellationToken);
            SendPayload(sendPayload);
            await receivePayloadTask.ConfigureAwait(false);
        }

        public async Task DoHandshakeAsync(CancellationToken cancellationToken)
        {
            // Transaction ID should be at 0xff
            if (_transactionId != 0xff)
            {
                throw new NetSyncException("Invalid transaction ID value");
            }
            
            // Wait for wakeup
            await ReceivePayloadAsync(payload =>
            {
                if (payload.Buffer.Length != NetSyncHandshakeWakeup.Length)
                {
                    throw new NetSyncException("Unexpected packet while waiting for NetSync wakeup");
                }
            }, cancellationToken).ConfigureAwait(false);
            
            // First handshake transaction
            await ExecuteTransactionAsync(
                new NetSyncPayload(new ReadOnlySequence<byte>(NetSyncHandshakeRequest1)),
                (payload) =>
                {
                    if (payload.Buffer.Length != NetSyncHandshakeResponse1.Length)
                    {
                        throw new NetSyncException("Unexpected response to first NetSync handshake packet");
                    }
                },
                cancellationToken).ConfigureAwait(false);
            
            // Second handshake transaction
            await ExecuteTransactionAsync(
                new NetSyncPayload(new ReadOnlySequence<byte>(NetSyncHandshakeRequest2)),
                (payload) =>
                {
                    if (payload.Buffer.Length != NetSyncHandshakeResponse2.Length)
                    {
                        throw new NetSyncException("Unexpected response to second NetSync handshake packet");
                    }
                },
                cancellationToken).ConfigureAwait(false);
        }

        private async Task ReceivePayloadAsync(Action<NetSyncPayload> handlePayloadAction, CancellationToken cancellationToken)
        {
            TaskCompletionSource receiveTcs = new();

            void NetSyncPacketReceived(object? sender, NetSyncPacketTransmittedEventArgs e)
            {
                try
                {
                    if (e.Packet.TransactionId != _transactionId)
                        throw new NetSyncException("Unexpected packet transaction ID received");

                    handlePayloadAction(new NetSyncPayload(e.Packet.Data));
                    receiveTcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    receiveTcs.TrySetException(ex);
                }
            }

            _device.ReceivedPacket += NetSyncPacketReceived;
            await using (cancellationToken.Register(() => { receiveTcs.TrySetCanceled(); }))
            {
                try
                {
                    await receiveTcs.Task.ConfigureAwait(false);
                }
                finally
                {
                    _device.ReceivedPacket -= NetSyncPacketReceived;
                }
            }
        }

        private void SendPayload(NetSyncPayload payload)
        {
            _device.SendPacket(new NetSyncPacket(_transactionId, payload.Buffer));
        }
    }
}