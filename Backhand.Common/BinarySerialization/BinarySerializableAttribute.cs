using System;

namespace Backhand.Common.BinarySerialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class BinarySerializableAttribute : Attribute
    {
        public Endian Endian { get => _endian ?? default; set => _endian = value; }
        public string MinimumLengthProperty { get => _minimumLengthProperty ?? string.Empty; set => _minimumLengthProperty = value; }

        public bool EndianSpecified => _endian.HasValue;
        public bool MinimumLengthPropertySpecified => !string.IsNullOrEmpty(_minimumLengthProperty);

        private Endian? _endian;
        private string? _minimumLengthProperty;
    }
}
