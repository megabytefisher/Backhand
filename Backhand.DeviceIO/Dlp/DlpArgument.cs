using Backhand.DeviceIO.Utility;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp
{
    public abstract class DlpArgument
    {
        protected const int DlpDateTimeLength = 8;

        public abstract int GetSerializedLength();
        public abstract int Serialize(Span<byte> buffer);
        public abstract SequencePosition Deserialize(ReadOnlySequence<byte> buffer);

        protected static DateTime ReadDlpDateTime(ref SequenceReader<byte> reader)
        {
            if (reader.Remaining < 8)
                throw new DlpException("Not enough data to read DLP date time");

            ushort year = reader.ReadUInt16BigEndian();
            byte month = reader.Read();
            byte day = reader.Read();
            byte hour = reader.Read();
            byte minute = reader.Read();
            byte second = reader.Read();
            reader.Advance(1); // Padding byte

            return new DateTime(year != 0 ? year : 1900, month, day, hour, minute, second);
        }

        protected static void WriteDlpDateTime(DateTime value, Span<byte> buffer)
        {
            if (buffer.Length < 8)
                throw new DlpException("Not enough space to write DLP date time");

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(0, 2), Convert.ToUInt16(value.Year));
            buffer[2] = Convert.ToByte(value.Month);
            buffer[3] = Convert.ToByte(value.Day);
            buffer[4] = Convert.ToByte(value.Hour);
            buffer[5] = Convert.ToByte(value.Minute);
            buffer[6] = Convert.ToByte(value.Second);
            buffer[7] = 0;
        }

        protected static string ReadNullTerminatedString(ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadTo(out ReadOnlySequence<byte> stringSequence, 0x00))
                throw new DlpException("Failed to find string null terminator");

            return Encoding.ASCII.GetString(stringSequence);
        }
    }
}
