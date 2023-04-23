using System;

namespace Backhand.Common.BinarySerialization
{
    public class GenerateBinarySerializationAttribute : Attribute
    {
        public Endian Endian { get; set; } = Endian.Big;
        public string MinimumLengthProperty { get; set; } = string.Empty;
    }
}
