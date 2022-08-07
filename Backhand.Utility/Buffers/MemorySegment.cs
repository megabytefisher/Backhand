using System.Buffers;

namespace Backhand.Utility.Buffers
{
    // https://www.stevejgordon.co.uk/creating-a-readonlysequence-from-array-data-in-dotnet
    public class MemorySegment<T> : ReadOnlySequenceSegment<T>
    {
        public MemorySegment(ReadOnlyMemory<T> memory)
        {
            Memory = memory;
        }

        public MemorySegment<T> Append(ReadOnlyMemory<T> memory)
        {
            var segment = new MemorySegment<T>(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = segment;

            return segment;
        }
    }
}
