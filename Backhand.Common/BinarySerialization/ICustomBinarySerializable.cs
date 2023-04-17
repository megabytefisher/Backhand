using Backhand.Common.Buffers;
using System.Buffers;

namespace Backhand.Common.BinarySerialization
{
    public interface ICustomBinarySerializable
    {
        int GetSize();
        void Serialize(ref SpanWriter<byte> bufferWriter);
        void Deserialize(ref SequenceReader<byte> bufferReader);
    }
}
