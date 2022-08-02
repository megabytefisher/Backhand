using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Utility
{
    public class SegmentBuffer : IDisposable
    {
        public int Length { get; private set; }

        private ArrayPool<byte> _arrayPool;
        private List<byte[]> _arrays;
        private MemorySegment<byte>? _first;
        private MemorySegment<byte>? _last;

        public SegmentBuffer(ArrayPool<byte>? arrayPool = null)
        {
            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
            Length = 0;
            _arrays = new List<byte[]>();
        }

        public void Dispose()
        {
            foreach (byte[] array in _arrays)
            {
                _arrayPool.Return(array);
            }
            _arrays.Clear();
            Length = 0;
            _first = null;
            _last = null;
        }

        public Span<byte> Append(int length)
        {
            Length += length;
            byte[] newArray = _arrayPool.Rent(length);
            _arrays.Add(newArray);

            if (_first == null)
            {
                _first = new MemorySegment<byte>(new ReadOnlyMemory<byte>(newArray, 0, length));
                _last = _first;
            }
            else
            {
                _last = _last!.Append(new ReadOnlyMemory<byte>(newArray, 0, length));
            }

            return new Span<byte>(newArray, 0, length);
        }

        public ReadOnlySequence<byte> AsSequence()
        {
            if (_first == null || _last == null)
                return ReadOnlySequence<byte>.Empty;

            return new ReadOnlySequence<byte>(_first, 0, _last, _last.Memory.Length);
        }
    }
}
