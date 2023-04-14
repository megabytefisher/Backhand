using System;
using System.Buffers;
using System.Collections.Generic;

namespace Backhand.Common.Buffers
{
    public class SegmentBuffer<T> : IDisposable
    {
        public int Length { get; private set; }

        private readonly List<T[]> _arrays;
        private MemorySegment<T>? _head;
        private MemorySegment<T>? _tail;

        private readonly ArrayPool<T> _arrayPool;

        public SegmentBuffer(ArrayPool<T>? arrayPool = null)
        {
            Length = 0;
            _arrays = new List<T[]>();
            _arrayPool = arrayPool ?? ArrayPool<T>.Shared;
        }

        public void Dispose()
        {
            foreach (T[] array in _arrays)
            {
                _arrayPool.Return(array);
            }

            _arrays.Clear();
            Length = 0;
            _head = null;
            _tail = null;
        }

        public Span<T> Append(int length)
        {
            Length += length;
            T[] newArray = _arrayPool.Rent(length);
            _arrays.Add(newArray);

            if (_head == null || _tail == null)
            {
                _head = new MemorySegment<T>(new ReadOnlyMemory<T>(newArray, 0, length));
                _tail = _head;
            }
            else
            {
                _tail = _tail.Append(new ReadOnlyMemory<T>(newArray, 0, length));
            }

            return new Span<T>(newArray, 0, length);
        }

        public ReadOnlySequence<T> AsReadOnlySequence()
        {
            if (_head == null || _tail == null)
            {
                return ReadOnlySequence<T>.Empty;
            }

            return new ReadOnlySequence<T>(_head, 0, _tail, _tail.Memory.Length);
        }
    }
}
