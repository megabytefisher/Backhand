using Backhand.Common.BinarySerialization;
using Backhand.Common.Buffers;
using Backhand.Protocols.Cmp.Internal;
using Backhand.Protocols.Padp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Protocols.Cmp
{
    public class CmpConnection
    {
        private readonly PadpConnection _padpConnection;
        private readonly ArrayPool<byte> _arrayPool;
        private readonly ILogger _logger;

        private static readonly CmpInitPacket BlankInitPacket = new();
        private static readonly int InitPacketSize = BinarySerializer<CmpInitPacket>.GetSize(BlankInitPacket);

        private const byte CmpMajorVersion = 1;
        private const byte CmpMinorVersion = 1;

        internal enum CmpPacketType : byte
        {
            WakeUp      = 0x01,
            Init        = 0x02,
            Abort       = 0x03,
            Extended    = 0x04,
        }

        [Flags]
        internal enum CmpInitPacketFlags : byte
        {
            None                            = 0b00000000,
            ShouldChangeBaudRate            = 0b10000000,
            ShouldUseOneMinuteTimeout       = 0b01000000,
            ShouldUseTwoMinuteTimeout       = 0b00100000,
            IsLongFormPadpHeaderSupported   = 0b00010000,
        }

        public CmpConnection(PadpConnection padpConnection, ArrayPool<byte>? arrayPool = null, ILogger? logger = null)
        {
            _padpConnection = padpConnection;
            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
            this._logger = logger ?? NullLogger.Instance;
        }

        public async Task WaitForWakeUpAsync(CancellationToken cancellationToken = default)
        {
            _logger.ListeningForWakeUp();
            await _padpConnection.ReceivePayloadAsync(0xff, (payload) =>
            {
                SequenceReader<byte> payloadReader = new(payload);
                if (payloadReader.Read() != (byte)CmpPacketType.WakeUp)
                    throw new CmpException("Unexpected data received while waiting for device wake up");
                _logger.WakeUpReceived();
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task DoHandshakeAsync(uint? newBaudRate = null, CancellationToken cancellationToken = default)
        {
            byte[] initPacketBuffer = _arrayPool.Rent(InitPacketSize);

            try
            {
                CmpInitPacket initPacket = new CmpInitPacket
                {
                    MajorVersion = CmpMajorVersion,
                    MinorVersion = CmpMinorVersion,
                    Flags = CmpInitPacketFlags.None | (newBaudRate.HasValue ? CmpInitPacketFlags.ShouldChangeBaudRate : CmpInitPacketFlags.None),
                    NewBaudRate = newBaudRate ?? 0,
                };

                _logger.SendingInitPacket(initPacket);
                BinarySerializer<CmpInitPacket>.Serialize(initPacket, initPacketBuffer);
                await _padpConnection.SendPayloadAsync(0xff, new ReadOnlySequence<byte>(initPacketBuffer, 0, InitPacketSize), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _arrayPool.Return(initPacketBuffer);
            }
        }

        [BinarySerializable]
        internal class CmpInitPacket
        {
            [BinarySerialize]
            public CmpPacketType Type { get; set; } = CmpPacketType.Init;

            [BinarySerialize]
            public CmpInitPacketFlags Flags { get; set; }

            [BinarySerialize]
            public byte MajorVersion { get; set; }

            [BinarySerialize]
            public byte MinorVersion { get; set; }

            [BinarySerialize]
            private byte Padding1 { get; set; }

            [BinarySerialize]
            private byte Padding2 { get; set; }

            [BinarySerialize]
            public uint NewBaudRate { get; set; }
        }
    }
}
