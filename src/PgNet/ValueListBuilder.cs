using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace PgNet
{
    internal ref struct ValueListBuilder<T>
    {
        private Span<T> _span;
        private T[]? _arrayFromPool;
        private int _pos;

        public ValueListBuilder(Span<T> initialSpan)
        {
            _span = initialSpan;
            _arrayFromPool = null;
            _pos = 0;
        }

        public int Length
        {
            get => _pos;
            set => _pos = value;
        }

        public ref T this[int index] => ref _span[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(T item)
        {
            var pos = _pos;
            if (pos >= _span.Length)
                Grow();

            _span[pos] = item;
            _pos = pos + 1;
        }

        public ReadOnlySpan<T> AsSpan()
        {
            return _span.Slice(0, _pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_arrayFromPool != null)
            {
                ArrayPool<T>.Shared.Return(_arrayFromPool);
                _arrayFromPool = null;
            }
        }

        private void Grow()
        {
            var array = ArrayPool<T>.Shared.Rent(_span.Length * 2);

            _span.TryCopyTo(array);

            var toReturn = _arrayFromPool;
            _span = _arrayFromPool = array;
            if (toReturn != null)
            {
                ArrayPool<T>.Shared.Return(toReturn);
            }
        }
    }
}
