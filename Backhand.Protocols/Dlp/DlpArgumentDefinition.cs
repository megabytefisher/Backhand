using Backhand.Common.BinarySerialization;
using Backhand.Common.Buffers;
using System;
using System.Buffers;

namespace Backhand.Protocols.Dlp
{
    public abstract class DlpArgumentDefinition
    {
        public Type Type { get; set; }
        public bool IsOptional { get; set; }

        protected DlpArgumentDefinition(Type type, bool isOptional = false)
        {
            Type = type;
            IsOptional = isOptional;
        }

        public abstract int GetSerializedSize(object value);
        public abstract void Serialize(ref SpanWriter<byte> writer, object value);
        public abstract void Deserialize(ref SequenceReader<byte> reader, object value);
    }

    public class DlpArgumentDefinition<T> : DlpArgumentDefinition where T : IBinarySerializable, new()
    {
        public DlpArgumentDefinition(bool isOptional = false) : base(typeof(T), isOptional)
        {
        }

        public int GetSerializedSize(T value)
        {
            return BinarySerializer<T>.GetSize(value);
        }

        public override int GetSerializedSize(object value)
        {
            if (value is T typedValue)
            {
                return GetSerializedSize(typedValue);
            }
            else
            {
                throw new Exception("Unhandled value type");
            }
        }

        public void Serialize(ref SpanWriter<byte> writer, T value)
        {
            BinarySerializer<T>.Serialize(value, ref writer);
        }

        public override void Serialize(ref SpanWriter<byte> writer, object value)
        {
            if (value is T typedValue)
            {
                Serialize(ref writer, typedValue);
            }
            else
            {
                throw new Exception("Unhandled value type");
            }
        }

        public void Deserialize(ref SequenceReader<byte> reader, T value)
        {
            BinarySerializer<T>.Deserialize(ref reader, value);
        }

        public override void Deserialize(ref SequenceReader<byte> reader, object value)
        {
            if (value is T typedValue)
            {
                Deserialize(ref reader, typedValue);
            }
            else
            {
                throw new Exception("Unhandled value type");
            }
        }
    }
}
