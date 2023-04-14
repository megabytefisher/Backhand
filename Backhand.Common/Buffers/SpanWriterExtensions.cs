using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Backhand.Common.Buffers
{
    public static class SpanWriterExtensions
    {
        public static void Write<T>(this ref SpanWriter<T> writer, T value) where T : unmanaged
        {
            if (!writer.TryWrite(value))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteRange<T>(this ref SpanWriter<T> writer, ReadOnlySpan<T> span) where T : unmanaged
        {
            if (!writer.TryWriteRange(span))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteRange<T>(this ref SpanWriter<T> writer, ReadOnlySequence<T> sequence) where T : unmanaged
        {
            if (!writer.TryWriteRange(sequence))
            {
                throw new BufferWriteException();
            }
        }

        private static unsafe bool TryWrite<T>(ref this SpanWriter<byte> writer, T value) where T : unmanaged
        {
            Span<byte> currentSpan = writer.RemainingSpan;
            if (currentSpan.Length < sizeof(T))
            {
                return false;
            }

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(currentSpan), value);
            writer.Advance(sizeof(T));
            return true;
        }

        public static bool TryWriteLittleEndian(ref this SpanWriter<byte> writer, short value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return writer.TryWrite(value);
        }

        public static bool TryWriteBigEndian(ref this SpanWriter<byte> writer, short value)
        {
            if (BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return writer.TryWrite(value);
        }

        public static bool TryWriteLittleEndian(ref this SpanWriter<byte> writer, ushort value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return writer.TryWrite(value);
        }

        public static bool TryWriteBigEndian(ref this SpanWriter<byte> writer, ushort value)
        {
            if (BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return writer.TryWrite(value);
        }

        public static bool TryWriteLittleEndian(ref this SpanWriter<byte> writer, int value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return writer.TryWrite(value);
        }

        public static bool TryWriteBigEndian(ref this SpanWriter<byte> writer, int value)
        {
            if (BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return writer.TryWrite(value);
        }

        public static bool TryWriteLittleEndian(ref this SpanWriter<byte> writer, uint value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return writer.TryWrite(value);
        }

        public static bool TryWriteBigEndian(ref this SpanWriter<byte> writer, uint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return writer.TryWrite(value);
        }

        public static bool TryWriteLittleEndian(ref this SpanWriter<byte> writer, long value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return writer.TryWrite(value);
        }

        public static bool TryWriteBigEndian(ref this SpanWriter<byte> writer, long value)
        {
            if (BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return writer.TryWrite(value);
        }

        public static bool TryWriteLittleEndian(ref this SpanWriter<byte> writer, ulong value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return writer.TryWrite(value);
        }

        public static bool TryWriteBigEndian(ref this SpanWriter<byte> writer, ulong value)
        {
            if (BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return writer.TryWrite(value);
        }

        public static void WriteInt16LittleEndian(ref this SpanWriter<byte> writer, short value)
        {
            if (!writer.TryWriteLittleEndian(value))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteInt16BigEndian(ref this SpanWriter<byte> writer, short value)
        {
            if (!writer.TryWriteBigEndian(value))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteUInt16LittleEndian(ref this SpanWriter<byte> writer, ushort value)
        {
            if (!writer.TryWriteLittleEndian(value))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteUInt16BigEndian(ref this SpanWriter<byte> writer, ushort value)
        {
            if (!writer.TryWriteBigEndian(value))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteInt32LittleEndian(ref this SpanWriter<byte> writer, int value)
        {
            if (!writer.TryWriteLittleEndian(value))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteInt32BigEndian(ref this SpanWriter<byte> writer, int value)
        {
            if (!writer.TryWriteBigEndian(value))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteUInt32LittleEndian(ref this SpanWriter<byte> writer, uint value)
        {
            if (!writer.TryWriteLittleEndian(value))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteUInt32BigEndian(ref this SpanWriter<byte> writer, uint value)
        {
            if (!writer.TryWriteBigEndian(value))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteInt64LittleEndian(ref this SpanWriter<byte> writer, long value)
        {
            if (!writer.TryWriteLittleEndian(value))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteInt64BigEndian(ref this SpanWriter<byte> writer, long value)
        {
            if (!writer.TryWriteBigEndian(value))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteUInt64LittleEndian(ref this SpanWriter<byte> writer, ulong value)
        {
            if (!writer.TryWriteLittleEndian(value))
            {
                throw new BufferWriteException();
            }
        }

        public static void WriteUInt64BigEndian(ref this SpanWriter<byte> writer, ulong value)
        {
            if (!writer.TryWriteBigEndian(value))
            {
                throw new BufferWriteException();
            }
        }
    }
}
