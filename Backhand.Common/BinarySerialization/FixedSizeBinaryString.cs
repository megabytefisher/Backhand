using System;
using System.Text;

namespace Backhand.Common.BinarySerialization
{
    [GenerateBinarySerialization]
    public partial class FixedSizeBinaryString : IBinarySerializable
    {
        [BinarySerialize]
        public byte[] Bytes { get; set; }

        public int Size
        {
            get => Bytes.Length;
            set => Bytes = new byte[value];
        }

        public Encoding Encoding { get; init; } = Encoding.ASCII;
        public bool NullTerminated { get; init; } = false;
        public byte PaddingByte { get; init; } = 0x00;

        public FixedSizeBinaryString(int size = 0)
        {
            Size = size;
            Bytes = new byte[size];
        }
        
        public string Value
        {
            get
            {
                if (Size == 0)
                {
                    return string.Empty;
                }

                if (NullTerminated)
                {
                    NullTerminatedBinaryString nullTerminatedString = new()
                    {
                        Encoding = Encoding
                    };

                    BinarySerializer<NullTerminatedBinaryString>.Deserialize(Bytes, nullTerminatedString);
                    return nullTerminatedString.Value;
                }
                else
                {
                    return Encoding.GetString(Bytes);
                }
            }
            set
            {
                if (value == string.Empty)
                {
                    Bytes = Array.Empty<byte>();
                    return;
                }

                if (NullTerminated)
                {
                    NullTerminatedBinaryString nullTerminatedString = new()
                    {
                        Encoding = Encoding,
                        Value = value
                    };

                    BinarySerializer<NullTerminatedBinaryString>.Serialize(nullTerminatedString, Bytes);
                }
                else
                {
                    Span<byte> bytes = Bytes.AsSpan();
                    int written = Encoding.GetBytes(value, bytes);
                    bytes.Slice(written).Fill(PaddingByte);
                }
            }
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(FixedSizeBinaryString fixedSizeBinaryString)
        {
            return fixedSizeBinaryString.Value;
        }
    }
}