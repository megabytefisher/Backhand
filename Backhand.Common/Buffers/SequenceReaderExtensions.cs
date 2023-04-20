using System;
using System.Buffers;

namespace Backhand.Common.Buffers
{
    public static class SequenceReaderExtensions
    {
        public static T Read<T>(this ref SequenceReader<T> reader) where T : unmanaged, IEquatable<T>
        {
            if (!reader.TryRead(out T value))
            {
                throw new BufferReadException();
            }
            return value;
        }

        public static ReadOnlySequence<T> ReadExact<T>(this ref SequenceReader<T> reader, int length) where T : unmanaged, IEquatable<T>
        {
            if (!reader.TryReadExact(length, out ReadOnlySequence<T> sequence))
            {
                throw new BufferReadException();
            }
            return sequence;
        }

        public static T Peek<T>(this ref SequenceReader<T> reader, long offset = 0) where T : unmanaged, IEquatable<T>
        {
            if (!reader.TryPeek(offset, out T value))
            {
                throw new BufferReadException();
            }
            return value;
        }

        public static ReadOnlySequence<T> ReadTo<T>(this ref SequenceReader<T> reader, T delimiter, bool advancePastDelimiter = true) where T : unmanaged, IEquatable<T>
        {
            if (!reader.TryReadTo(out ReadOnlySequence<T> sequence, delimiter, advancePastDelimiter))
            {
                throw new BufferReadException();
            }
            return sequence;
        }

        public static void AdvanceTo<T>(this ref SequenceReader<T> reader, T delimiter, bool advancePastDelimiter = true) where T : unmanaged, IEquatable<T>
        {
            if (!reader.TryAdvanceTo(delimiter, advancePastDelimiter))
            {
                throw new BufferReadException();
            }
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
                throw new BufferReadException();
            }
            return value;
        }

        public static int ReadInt32BigEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadBigEndian(out int value))
            {
                throw new BufferReadException();
            }
            return value;
        }

        public static long ReadInt64BigEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadBigEndian(out long value))
            {
                throw new BufferReadException();
            }
            return value;
        }

        public static ushort ReadUInt16BigEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadBigEndian(out ushort value))
            {
                throw new BufferReadException();
            }
            return value;
        }

        public static uint ReadUInt32BigEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadBigEndian(out uint value))
            {
                throw new BufferReadException();
            }
            return value;
        }

        public static ulong ReadUInt64BigEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadBigEndian(out ulong value))
            {
                throw new BufferReadException();
            }
            return value;
        }

        public static short ReadInt16LittleEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadLittleEndian(out short value))
            {
                throw new BufferReadException();
            }
            return value;
        }

        public static int ReadInt32LittleEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadLittleEndian(out int value))
            {
                throw new BufferReadException();
            }
            return value;
        }

        public static long ReadInt64LittleEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadLittleEndian(out long value))
            {
                throw new BufferReadException();
            }
            return value;
        }

        public static ushort ReadUInt16LittleEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadLittleEndian(out ushort value))
            {
                throw new BufferReadException();
            }
            return value;
        }

        public static uint ReadUInt32LittleEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadLittleEndian(out uint value))
            {
                throw new BufferReadException();
            }
            return value;
        }

        public static ulong ReadUInt64LittleEndian(this ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadLittleEndian(out ulong value))
            {
                throw new BufferReadException();
            }
            return value;
        }
    }
}
