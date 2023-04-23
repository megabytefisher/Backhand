using Backhand.Common.Buffers;
using System;
using System.Buffers;

namespace Backhand.Common.BinarySerialization
{
    public static class BinarySerializer<T> where T : IBinarySerializable
    {
        private static T? _defaultInstance;

        public static int GetSize(T value)
        {
            return value.GetSize();
        }

        public static int GetMinimumSize<TItem>() where TItem : T, new()
        {
            if (_defaultInstance == null)
            {
                _defaultInstance = new TItem();
            }

            return GetSize(_defaultInstance);
        }

        public static void Serialize(T value, ref SpanWriter<byte> buffer)
        {
            value.Write(ref buffer);
        }

        public static void Serialize(T value, Span<byte> buffer)
        {
            SpanWriter<byte> bufferWriter = new(buffer);
            Serialize(value, ref bufferWriter);
        }

        public static void Serialize(T value, byte[] buffer)
        {
            SpanWriter<byte> bufferWriter = new(buffer);
            Serialize(value, ref bufferWriter);
        }

        public static void Deserialize(ref SequenceReader<byte> buffer, T value)
        {
            value.Read(ref buffer);
        }

        public static void Deserialize(ReadOnlySequence<byte> buffer, T value)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);
            Deserialize(ref bufferReader, value);
        }

        public static void Deserialize(byte[] buffer, T value)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(new ReadOnlySequence<byte>(buffer));
            Deserialize(ref bufferReader, value);
        }
    }
}
