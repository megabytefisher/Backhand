using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Protocols.Dlp
{
    public static class DlpPrimitives
    {
        public const int DlpDateTimeSize = sizeof(byte) * 8;

        public static void WriteDlpDateTime(Span<byte> buffer, DateTime dateTime)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(0, 2), Convert.ToUInt16(dateTime.Year));
            buffer[2] = Convert.ToByte(dateTime.Month);
            buffer[3] = Convert.ToByte(dateTime.Day);
            buffer[4] = Convert.ToByte(dateTime.Hour);
            buffer[5] = Convert.ToByte(dateTime.Minute);
            buffer[6] = Convert.ToByte(dateTime.Second);
            buffer[7] = 0;
        }

        public static DateTime ReadDlpDateTime(ReadOnlySpan<byte> buffer)
        {
            ushort year = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(0, 2));
            byte month = buffer[2];
            byte day = buffer[3];
            byte hour = buffer[4];
            byte minute = buffer[5];
            byte second = buffer[6];

            return new DateTime(year, month, day, hour, minute, second);
        }
    }
}
