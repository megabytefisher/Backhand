using System;
using System.Buffers;
using System.Text;
using Backhand.Common.Buffers;

namespace Backhand.Common.BinarySerialization
{
    public class NullTerminatedBinaryString : IBinarySerializable
    {
        public Encoding Encoding { get; init; } = Encoding.ASCII;
        public byte[] Bytes { get; set; } = Array.Empty<byte>();

        public string Value
        {
            get => Encoding.GetString(Bytes, 0, Bytes.Length - NullBytes.Length);
            set => Bytes = Encoding.GetBytes(value + "\0");
        }

        private readonly byte[] NullBytes;

        public NullTerminatedBinaryString(string? defaultValue = null)
        {
            Value = defaultValue ?? string.Empty;
            NullBytes = Encoding.GetBytes("\0");
        }

        public int GetSize()
        {
            return Encoding.GetByteCount(Value) + Encoding.GetByteCount("\0");
        }

        public void Read(ref SequenceReader<byte> bufferReader)
        {
            if (bufferReader.TryReadTo(out ReadOnlySequence<byte> sequence, NullBytes, advancePastDelimiter: true))
            {
                Value = Encoding.GetString(sequence);
            }
            else
            {
                throw new Exception("Could not find null terminator");
            }
        }

        public void Write(ref SpanWriter<byte> bufferWriter)
        {
            Span<byte> remainingBytes = bufferWriter.RemainingSpan;
            int bytesWritten = Encoding.GetBytes(Value, remainingBytes);
            bufferWriter.Advance(bytesWritten);
            bufferWriter.Write(Encoding.GetBytes("\0"));
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