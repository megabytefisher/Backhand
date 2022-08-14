using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Backhand.Pdb.Utility
{
    public static class BufferUtilities
    {
        public enum DatabaseTimestampEpoch
        {
            Palm,
            Unix
        }

        public const int DatabaseTimestampLength = 4;

        private static readonly DateTime PalmEpoch = new(1904, 1, 1);
        private static readonly DateTime UnixEpoch = new(1970, 1, 1);

        public static DateTime ReadDatabaseTimestamp(ref SequenceReader<byte> bufferReader)
        {
            uint offsetSeconds = bufferReader.ReadUInt32BigEndian();

            if (offsetSeconds == 0)
                return PalmEpoch;

            bool isPalmEpoch = (offsetSeconds & (1 << 31)) != 0;

            return isPalmEpoch ?
                PalmEpoch.AddSeconds(offsetSeconds) :
                UnixEpoch.AddSeconds(offsetSeconds);
        }

        public static int WriteDatabaseTimestamp(Span<byte> buffer, DateTime value, DatabaseTimestampEpoch epochType = DatabaseTimestampEpoch.Unix)
        {
            if (epochType == DatabaseTimestampEpoch.Palm)
            {
                if (value == DateTime.MinValue)
                    value = PalmEpoch;

                uint offsetSeconds = Convert.ToUInt32(value.Subtract(PalmEpoch).TotalSeconds);
                BinaryPrimitives.WriteUInt32BigEndian(buffer, offsetSeconds);
            }
            else
            {
                if (value == DateTime.MinValue)
                    value = UnixEpoch;

                int offsetSeconds = Convert.ToInt32(value.Subtract(UnixEpoch).TotalSeconds);
                BinaryPrimitives.WriteInt32BigEndian(buffer, offsetSeconds);
            }

            return DatabaseTimestampLength;
        }

        public static string ReadFixedLengthString(ref SequenceReader<byte> bufferReader, int length)
        {
            string value = Encoding.ASCII.GetString(bufferReader.Sequence.Slice(bufferReader.Position, length)).TrimEnd('\0');
            bufferReader.Advance(length);
            return value;
        }

        public static void WriteFixedLengthString(Span<byte> buffer, string value)
        {
            int offset = Encoding.ASCII.GetBytes(value, buffer);
            for (; offset < buffer.Length; offset++)
                buffer[offset] = 0;
        }
    }
}
