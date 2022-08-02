using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Utility
{
    internal static class SequenceReaderExtensions
    {
        public static byte Read(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryRead(out byte value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }

        public static byte Peek(this ref SequenceReader<byte> reader, long offset = 0)
        {
            if (!reader.TryPeek(offset, out byte value))
            {
                throw new Exception("Failed to peek value from sequence reader");
            }
            return value;
        }

        public static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out ushort value)
        {
            bool success = reader.TryReadBigEndian(out short valueSigned);
            value = (ushort)valueSigned;
            return success;
        }

        public static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out uint value)
        {
            bool success = reader.TryReadBigEndian(out int valueSigned);
            value = (uint)valueSigned;
            return success;
        }

        public static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out ulong value)
        {
            bool success = reader.TryReadBigEndian(out long valueSigned);
            value = (ulong)valueSigned;
            return success;
        }

        public static bool TryReadLittleEndian(this ref SequenceReader<byte> reader, out ushort value)
        {
            bool success = reader.TryReadLittleEndian(out short valueSigned);
            value = (ushort)valueSigned;
            return success;
        }

        public static bool TryReadLittleEndian(this ref SequenceReader<byte> reader, out uint value)
        {
            bool success = reader.TryReadLittleEndian(out int valueSigned);
            value = (uint)valueSigned;
            return success;
        }

        public static bool TryReadLittleEndian(this ref SequenceReader<byte> reader, out ulong value)
        {
            bool success = reader.TryReadLittleEndian(out long valueSigned);
            value = (ulong)valueSigned;
            return success;
        }

        public static short ReadInt16BigEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadBigEndian(out short value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }

        public static int ReadInt32BigEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadBigEndian(out int value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }

        public static long ReadInt64BigEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadBigEndian(out long value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }

        public static ushort ReadUInt16BigEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadBigEndian(out ushort value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }

        public static uint ReadUInt32BigEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadBigEndian(out uint value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }

        public static ulong ReadUInt64BigEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadBigEndian(out ulong value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }

        public static short ReadInt16LittleEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadLittleEndian(out short value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }

        public static int ReadInt32LittleEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadLittleEndian(out int value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }

        public static long ReadInt64LittleEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadLittleEndian(out long value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }

        public static ushort ReadUInt16LittleEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadLittleEndian(out ushort value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }

        public static uint ReadUInt32LittleEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadLittleEndian(out uint value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }

        public static ulong ReadUInt64LittleEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadLittleEndian(out ulong value))
            {
                throw new Exception("Failed to read value from sequence reader");
            }
            return value;
        }
    }
}
