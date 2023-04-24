using System;

namespace Backhand.Common.BinarySerialization.Generation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateBinarySerializationAttribute : Attribute
    {
        public Endian Endian { get; set; } = Generation.Endian.Big;
        public string MinimumLengthProperty { get; set; } = string.Empty;
    }
}
