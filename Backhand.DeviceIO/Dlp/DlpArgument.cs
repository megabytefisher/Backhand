using System;
using Backhand.Utility.Buffers;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Backhand.DeviceIO.Dlp
{
    public abstract class DlpArgument
    {
        protected const int DlpDateTimeLength = 8;

        /* Should be implemented by request arguments */
        public virtual int GetSerializedLength()
        {
            throw new NotImplementedException();
        }

        public virtual int Serialize(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }
        
        /* Should be implemented by response arguments */
        public virtual SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }

        /* Common serialization methods */
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

            return year == 0 ? DateTime.MinValue : new DateTime(year, month, day, hour, minute, second);
        }

        protected static int WriteDlpDateTime(Span<byte> buffer, DateTime value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(0, 2), Convert.ToUInt16(value.Year));
            buffer[2] = Convert.ToByte(value.Month);
            buffer[3] = Convert.ToByte(value.Day);
            buffer[4] = Convert.ToByte(value.Hour);
            buffer[5] = Convert.ToByte(value.Minute);
            buffer[6] = Convert.ToByte(value.Second);
            buffer[7] = 0;

            return DlpDateTimeLength;
        }

        protected static int GetNullTerminatedStringLength(string value)
        {
            return Encoding.ASCII.GetByteCount(value) + 1;
        }

        protected static string ReadNullTerminatedString(ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadTo(out ReadOnlySequence<byte> stringSequence, 0x00))
                throw new DlpException("Failed to find string null terminator");

            return Encoding.ASCII.GetString(stringSequence);
        }

        protected static int WriteNullTerminatedString(Span<byte> buffer, string value)
        {
            int offset = Encoding.ASCII.GetBytes(value, buffer);
            buffer[offset++] = 0x00;

            return offset;
        }

        protected static string ReadFixedLengthString(ref SequenceReader<byte> reader, int length)
        {
            string result = Encoding.ASCII.GetString(reader.Sequence.Slice(reader.Position, length)).TrimEnd('\0');
            reader.Advance(length);
            return result;
        }

        protected static void WriteFixedLengthString(Span<byte> buffer, string value)
        {
            int offset = Encoding.ASCII.GetBytes(value, buffer);
            for (; offset < buffer.Length; offset++)
                buffer[offset] = 0;
        }
    }
}
