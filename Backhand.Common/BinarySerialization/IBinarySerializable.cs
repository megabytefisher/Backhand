using System.Buffers;
using Backhand.Common.Buffers;

namespace Backhand.Common.BinarySerialization;

public interface IBinarySerializable
{
    int GetSize();
    void Write(ref SpanWriter<byte> bufferWriter);
    void Read(ref SequenceReader<byte> bufferReader);
}