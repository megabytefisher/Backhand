using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp
{
    public abstract class DlpArgumentDefinition
    {
        public Type Type { get; private init; }
        public bool IsOptional { get; private init; }

        public DlpArgumentDefinition(Type type, bool isOptional = false)
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
