using Backhand.DeviceIO.Padp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private PadpConnection _padp;

        private const byte CmpMajorVersion = 1;
        private const byte CmpMinorVersion = 1;

        public CmpConnection(PadpConnection padp)
        {
            _padp = padp;
        }

        public async Task DoHandshakeAsync(uint? newBaudRate = null)
        {
            _padp.BumpTransactionId();

            byte[] initBuffer = ArrayPool<byte>.Shared.Rent(10);
            WriteInit(initBuffer, newBaudRate);
            await _padp.SendData((new ReadOnlySequence<byte>(initBuffer)).Slice(0, 10));
            ArrayPool<byte>.Shared.Return(initBuffer);
        }

        public async Task WaitForWakeUpAsync(CancellationToken cancellationToken = default)
        {
            TaskCompletionSource wakeUpTcs = new TaskCompletionSource();

            Action<object?, PadpDataReceivedEventArgs> wakeUpReceiver = (sender, e) =>
            {
                SequenceReader<byte> packetReader = new SequenceReader<byte>(e.Data);

                if (packetReader.Read() != (byte)CmpPacketType.WakeUp)
                    return;

                wakeUpTcs.TrySetResult();
            };

            _padp.ReceivedData += wakeUpReceiver.Invoke;

            using (cancellationToken.Register(() =>
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

        private void WriteInit(Span<byte> buffer, uint? newBaudRate = null)
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
