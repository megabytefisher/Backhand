using System;
using System.Linq;
using System.Reflection;
using Backhand.Common.BinarySerialization;

namespace Backhand.Protocols.Dlp
{
    public abstract class DlpArgument
    {
        public override string ToString() => "[" + GetLoggingStringInternal() + "]";

        protected virtual string GetLoggingStringInternal() =>
            string.Join(
                ", ",
                GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(p => p.GetCustomAttribute<BinarySerializeAttribute>() != null).Select(
                    p => $"{p.Name}={(p.PropertyType.IsArray ? string.Join(", ", ((Array)p.GetValue(this)!).Cast<object>()) : p.GetValue(this))}"
                )
            );
    }
}
