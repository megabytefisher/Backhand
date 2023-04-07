using System.Buffers;

namespace Backhand.Common.Buffers
{
    public ref struct SpanWriter<T> where T : unmanaged
    {
        public Span<T> Span { get; }
        public int Index { readonly get; private set; }

        public readonly Span<T> RemainingSpan => Span.Slice(Index);
        public readonly int Length => Span.Length;
        public readonly int Remaining => Length - Index;

        public SpanWriter(Span<T> span)
        {
            Span = span;
            Index = 0;
        }

        public bool TryWrite(T value)
        {
            if (Remaining < 1)
            {
                return false;
            }

            Span[Index] = value;
            Index++;
            return true;
        }

        public void Advance(int count)
        {
            Index += count;
        }

        public void Rewind(int count)
        {
            Index -= count;
        }

        public bool TryWriteRange(ReadOnlySpan<T> span)
        {
            if (Remaining < span.Length)
            {
                return false;
            }

            span.CopyTo(RemainingSpan);
            Index += span.Length;
            return true;
        }

        public bool TryWriteRange(ReadOnlySequence<T> sequence)
        {
            if (Remaining < sequence.Length)
            {
                return false;
            }

            sequence.CopyTo(RemainingSpan);
            Index += Convert.ToInt32(sequence.Length);
            return true;
        }
    }
}
