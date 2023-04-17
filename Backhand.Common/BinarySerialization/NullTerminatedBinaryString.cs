using System;
using System.Buffers;
using System.Text;
using Backhand.Common.Buffers;

namespace Backhand.Common.BinarySerialization
{
    [BinarySerializable]
    public class NullTerminatedBinaryString : ICustomBinarySerializable
    {
        public string Value { get; set; } = string.Empty;

        public Encoding Encoding { get; init; } = Encoding.ASCII;

        public NullTerminatedBinaryString()
        {
        }

        public int GetSize()
        {
            return Encoding.GetByteCount(Value) + Encoding.GetByteCount("\0");
        }

        public void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            byte[] nullBytes = Encoding.GetBytes("\0");

            if (bufferReader.TryReadTo(out ReadOnlySequence<byte> sequence, nullBytes, advancePastDelimiter: true))
            {
                Value = Encoding.GetString(sequence);
            }
            else
            {
                throw new Exception("Could not find null terminator");
            }
        }

        public void Serialize(ref SpanWriter<byte> bufferWriter)
        {
            Span<byte> remainingBytes = bufferWriter.RemainingSpan;
            int bytesWritten = Encoding.GetBytes(Value, remainingBytes);
            bufferWriter.Advance(bytesWritten);
            bufferWriter.WriteRange(Encoding.GetBytes("\0"));
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(NullTerminatedBinaryString value)
        {
            return value.Value;
        }
    }
}