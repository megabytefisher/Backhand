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
                await _padp.SendData((new ReadOnlySequence<byte>(initBuffer)).Slice(0, 10), cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(initBuffer);
            }
        }

        public async Task WaitForWakeUpAsync(CancellationToken cancellationToken = default)
        {
            TaskCompletionSource wakeUpTcs = new();

            Action<object?, PadpDataReceivedEventArgs> wakeUpReceiver = (_, e) =>
            {
                SequenceReader<byte> packetReader = new(e.Data);

                if (packetReader.Read() != (byte)CmpPacketType.WakeUp)
                    return;

                wakeUpTcs.TrySetResult();
            };

            _padp.ReceivedData += wakeUpReceiver.Invoke;

            await using (cancellationToken.Register(() =>
                         {
                             wakeUpTcs.TrySetCanceled();
                         }))
            {
                try
                {
                    await wakeUpTcs.Task;
                }
                finally
                {
                    _padp.ReceivedData -= wakeUpReceiver.Invoke;
                }
            }
        }

        private static void WriteInit(Span<byte> buffer, uint? newBaudRate = null)
        {
            buffer[0] = (byte)CmpPacketType.Init;
            buffer[1] = (byte)(CmpInitPacketFlags.None | (newBaudRate != null ? CmpInitPacketFlags.ShouldChangeBaudRate : CmpInitPacketFlags.None));
            buffer[2] = CmpMajorVersion;
            buffer[3] = CmpMinorVersion;
            buffer[4] = 0x00;
            buffer[5] = 0x00;
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(6), (newBaudRate ?? 0)); // Baud rate
        }
    }
}
