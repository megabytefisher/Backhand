using System;

namespace Backhand.Common.BinarySerialization
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class BinarySerializeAttribute : Attribute
    {
    }
}
