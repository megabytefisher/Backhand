using System;

namespace Backhand.Common.BinarySerialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class BinarySerializableAttribute : Attribute
    {
        public Endian Endian { get => _endian ?? default; set => _endian = value; }
        public StringEncoding StringEncoding { get => _stringEncoding ?? default; set => _stringEncoding = value; }
        public string MinimumLengthProperty { get => _minimumLengthProperty ?? string.Empty; set => _minimumLengthProperty = value; }

        public bool EndianSpecified => _endian.HasValue;
        public bool StringEncodingSpecified => _stringEncoding.HasValue;
        public bool MinimumLengthPropertySpecified => !string.IsNullOrEmpty(_minimumLengthProperty);

        private Endian? _endian;
        private StringEncoding? _stringEncoding;
        private string? _minimumLengthProperty;
    }
}
