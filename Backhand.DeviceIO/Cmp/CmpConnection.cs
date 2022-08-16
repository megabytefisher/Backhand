using System;
using Backhand.DeviceIO.Padp;
using Backhand.Utility.Buffers;
using System.Buffers;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Cmp
{
    public class CmpConnection
    {
        private enum CmpPacketType : byte
        {
            WakeUp                          = 0x01,
            Init                            = 0x02,
            Abort                           = 0x03,
            Extended                        = 0x04,
        }

        [Flags]
        private enum CmpInitPacketFlags : byte
        {
            None                            = 0b00000000,
            ShouldChangeBaudRate            = 0b10000000,
            ShouldUseOneMinuteTimeout       = 0b01000000,
            ShouldUseTwoMinuteTimeout       = 0b00100000,
            IsLongFormPadpHeaderSupported   = 0b00010000,
        }

        private readonly PadpConnection _padp;

        private const byte CmpMajorVersion = 1;
        private const byte CmpMinorVersion = 1;

        public CmpConnection(PadpConnection padp)
        {
            _padp = padp;
        }

        public async Task DoHandshakeAsync(uint? newBaudRate = null, CancellationToken cancellationToken = default)
        {
            byte[] initBuffer = ArrayPool<byte>.Shared.Rent(10);

            try
            {
                WriteInit(initBuffer, newBaudRate);
                await _padp.SendPayloadAsync(
                    new PadpPayload(new ReadOnlySequence<byte>(initBuffer).Slice(0, 10)),
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(initBuffer);
            }
        }

        public async Task WaitForWakeUpAsync(CancellationToken cancellationToken = default)
        {
            await _padp.ReceivePayloadAsync(
                (padpPayload) =>
                {
                    SequenceReader<byte> payloadReader = new(padpPayload.Buffer);

                    if (payloadReader.Read() != (byte)CmpPacketType.WakeUp)
                        throw new PadpException("Unexpected data received while waiting for device wake up.");
                },
                cancellationToken).ConfigureAwait(false);
        }

        private static void WriteInit(Span<byte> buffer, uint? newBaudRate = null)
        {
            int offset = 0;
            
            buffer[offset] = (byte)CmpPacketType.Init;
            offset += sizeof(byte);
            
            buffer[offset] = (byte)(CmpInitPacketFlags.None | (newBaudRate != null ? CmpInitPacketFlags.ShouldChangeBaudRate : CmpInitPacketFlags.None));
            offset += sizeof(byte);
            
            buffer[offset] = CmpMajorVersion;
            offset += sizeof(byte);
            
            buffer[offset] = CmpMinorVersion;
            offset += sizeof(byte);
            
            buffer[offset] = 0x00;
            offset += sizeof(byte);
            
            buffer[offset] = 0x00;
            offset += sizeof(byte);
            
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, sizeof(uint)), (newBaudRate ?? 0)); // Baud rate
            offset += sizeof(uint);
        }
    }
}
