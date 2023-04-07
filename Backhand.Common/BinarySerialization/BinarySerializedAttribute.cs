using System.Linq.Expressions;
using System.Text;

namespace Backhand.Common.BinarySerialization
{
    public class BinarySerializedAttribute : Attribute
    {
        public Endian? Endian { get; }
        public StringEncoding? StringEncoding { get; }
        public string? LengthPropertyName { get; }
        public bool? NullTerminated { get; }

        public BinarySerializedAttribute()
        {
        }

        public BinarySerializedAttribute(Endian endian)
        {
            Endian = endian;
        }
    }
}
