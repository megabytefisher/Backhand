using System;
using System.Buffers;

namespace Backhand.DeviceIO.Dlp
{
    public abstract class DlpArgumentDefinition
    {
        public Type Type { get; }
        public bool IsOptional { get; }

        protected DlpArgumentDefinition(Type type, bool isOptional = false)
        {
            Type = type;
            IsOptional = isOptional;
        }

        public abstract DlpArgument Deserialize(ReadOnlySequence<byte> buffer);
    }

    public class DlpArgumentDefinition<T> : DlpArgumentDefinition where T : DlpArgument, new()
    {
        public DlpArgumentDefinition(bool isOptional = false)
            : base (typeof(T), isOptional)
        {

        }

        public override DlpArgument Deserialize(ReadOnlySequence<byte> buffer)
        {
            T result = new T();
            result.Deserialize(buffer);
            return result;
        }
    }

}
