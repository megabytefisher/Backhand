using Backhand.DeviceIO.Padp;
using Backhand.DeviceIO.Utility;
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

        public async Task DoHandshakeAsync()
        {
            await WaitForWakeUpAsync();

            _padp.BumpTransactionId();

            byte[] initBuffer = ArrayPool<byte>.Shared.Rent(10);
            WriteInit(initBuffer);
            await _padp.SendData(((Memory<byte>)initBuffer).Slice(0, 10));
            ArrayPool<byte>.Shared.Return(initBuffer);
        }

        private async Task WaitForWakeUpAsync()
        {
            using AsyncFlag wakeUpFlag = new AsyncFlag();

            Action<object?, PadpDataReceivedEventArgs> wakeUpReceiver = (sender, e) =>
            {
                SequenceReader<byte> packetReader = new SequenceReader<byte>(e.Data);

                if (packetReader.Read() != (byte)CmpPacketType.WakeUp)
                    return;

                wakeUpFlag.Set();
            };

            _padp.ReceivedData += wakeUpReceiver.Invoke;
            await wakeUpFlag.WaitAsync();
            _padp.ReceivedData -= wakeUpReceiver.Invoke;
        }

        private void WriteInit(Span<byte> buffer)
        {
            buffer[0] = (byte)CmpPacketType.Init;
            buffer[1] = (byte)CmpInitPacketFlags.None;
            buffer[2] = CmpMajorVersion;
            buffer[3] = CmpMinorVersion;
            buffer[4] = 0x00;
            buffer[5] = 0x00;
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(6), 0); // Baud rate
        }
    }
}
