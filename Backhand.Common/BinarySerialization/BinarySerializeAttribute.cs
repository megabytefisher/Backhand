using System;

namespace Backhand.Common.BinarySerialization
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class BinarySerializeAttribute : Attribute
    {
        public Endian Endian { get => _endian ?? default; set => _endian = value; }
        public string ConditionProperty { get => _conditionProperty ?? string.Empty; set => _conditionProperty = value; }

        public bool EndianSpecified => _endian.HasValue;
        public bool ConditionPropertySpecified => !string.IsNullOrEmpty(_conditionProperty);

        private Endian? _endian;
        private string? _conditionProperty;
    }
}
