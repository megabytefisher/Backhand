using System;

namespace Backhand.Common.BinarySerialization
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BinarySerializeAttribute : Attribute
    {
        public Endian Endian { get => _endian ?? default; set => _endian = value; }
        public StringEncoding StringEncoding { get => _stringEncoding ?? default; set => _stringEncoding = value; }
        public string LengthProperty { get => _lengthProperty ?? string.Empty; set => _lengthProperty = value; }
        public int Length { get => _length ?? default; set => _length = value; }
        public byte PaddingValue { get => _paddingValue ?? default; set => _paddingValue = value; }
        public string ConditonProperty { get => _conditionProperty ?? string.Empty; set => _conditionProperty = value; }
        public bool NullTerminated { get => _nullTerminated ?? default; set => _nullTerminated = value; }

        public bool EndianSpecified => _endian.HasValue;
        public bool StringEncodingSpecified => _stringEncoding.HasValue;
        public bool LengthPropertySpecified => !string.IsNullOrEmpty(_lengthProperty);
        public bool LengthSpecified => _length.HasValue;
        public bool PaddingValueSpecified => _paddingValue.HasValue;
        public bool ConditionPropertySpecified => !string.IsNullOrEmpty(_conditionProperty);
        public bool NullTerminatedSpecified => _nullTerminated.HasValue;

        private Endian? _endian;
        private StringEncoding? _stringEncoding;
        private string? _lengthProperty;
        private int? _length;
        private byte? _paddingValue;
        private string? _conditionProperty;
        private bool? _nullTerminated;
    }
}
