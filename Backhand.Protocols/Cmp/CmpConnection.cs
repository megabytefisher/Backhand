using Backhand.Common.Buffers;
using Backhand.Protocols.Padp;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Protocols.Cmp
{
    public static class CmpConnection
    {
        private const byte CmpMajorVersion = 1;
        private const byte CmpMinorVersion = 1;

        private const int InitPacketSize = 10;

        private enum CmpPacketType : byte
        {
            WakeUp = 0x01,
            Init = 0x02,
            Abort = 0x03,
            Extended = 0x04,
        }

        [Flags]
        private enum CmpInitPacketFlags : byte
        {
            None = 0b00000000,
            ShouldChangeBaudRate = 0b10000000,
            ShouldUseOneMinuteTimeout = 0b01000000,
            ShouldUseTwoMinuteTimeout = 0b00100000,
            IsLongFormPadpHeaderSupported = 0b00010000,
        }

        public static async Task WaitForWakeUpAsync(PadpConnection padpConnection, CancellationToken cancellationToken = default)
        {
            await padpConnection.ReceivePayloadAsync((payload) =>
            {
                SequenceReader<byte> payloadReader = new(payload);

                if (payloadReader.Read() != (byte)CmpPacketType.WakeUp)
                    throw new CmpException("Unexpected data received while waiting for device wake up");
            }, cancellationToken).ConfigureAwait(false);
        }

        public static async Task DoHandshakeAsync(PadpConnection padpConnection, uint? newBaudRate = null, ArrayPool<byte>? arrayPool = null, CancellationToken cancellationToken = default)
        {
            arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
            byte[] initPacketBuffer = arrayPool.Rent(InitPacketSize);

            try
            {
                WriteInitPacket(initPacketBuffer.AsSpan().Slice(0, InitPacketSize), newBaudRate);
                await padpConnection.SendPayloadAsync(new ReadOnlySequence<byte>(initPacketBuffer, 0, InitPacketSize), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                arrayPool.Return(initPacketBuffer);
            }
        }

        private static void WriteInitPacket(Span<byte> buffer, uint? newBaudRate = null)
        {
            if (buffer.Length != InitPacketSize)
                throw new CmpException("Buffer supplied for CMP init packet was incorrect size");

            SpanWriter<byte> bufferWriter = new SpanWriter<byte>(buffer);
            bufferWriter.Write((byte)CmpPacketType.Init);
            bufferWriter.Write((byte)(CmpInitPacketFlags.None | (newBaudRate != null ? CmpInitPacketFlags.ShouldChangeBaudRate : CmpInitPacketFlags.None)));
            bufferWriter.Write(CmpMajorVersion);
            bufferWriter.Write(CmpMinorVersion);
            bufferWriter.Write((byte)0);
            bufferWriter.Write((byte)0);
            bufferWriter.WriteUInt32BigEndian(newBaudRate ?? 0);
        }
    }
}
