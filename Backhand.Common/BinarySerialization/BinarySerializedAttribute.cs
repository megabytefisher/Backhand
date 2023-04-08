using System.Linq.Expressions;
using System.Text;

namespace Backhand.Common.BinarySerialization
{
    public class BinarySerializedAttribute : Attribute
    {
        public Endian Endian { get => _endian ?? default; set => _endian = value; }
        public StringEncoding StringEncoding { get => _stringEncoding ?? default; set => _stringEncoding = value; }
        public string LengthName { get => _lengthName ?? string.Empty; set => _lengthName = value; }
        public bool NullTerminated { get => _nullTerminated ?? default; set => _nullTerminated = value; }

        public bool EndianSpecified => _endian.HasValue;
        public bool StringEncodingSpecified => _stringEncoding.HasValue;
        public bool LengthNameSpecified => !string.IsNullOrEmpty(_lengthName);
        public bool NullTerminatedSpecified => _nullTerminated.HasValue;

        private Endian? _endian;
        private StringEncoding? _stringEncoding;
        private string? _lengthName;
        private bool? _nullTerminated;
    }
}
