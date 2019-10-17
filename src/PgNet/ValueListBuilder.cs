using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace PgNet
{
    internal ref struct ValueListBuilder<T>
    {
        private Span<T> m_span;
        private readonly ArrayPool<T> m_pool;
        private T[]? m_arrayFromPool;
        private int m_pos;

        public ValueListBuilder(Span<T> initialSpan, ArrayPool<T> pool)
        {
            m_span = initialSpan;
            m_pool = pool;
            m_arrayFromPool = null;
            m_pos = 0;
        }

        public int Length
        {
            get => m_pos;
            set => m_pos = value;
        }

        public ref T this[int index] => ref m_span[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(T item)
        {
            var pos = m_pos;
            if (pos >= m_span.Length)
                Grow();

            m_span[pos] = item;
            m_pos = pos + 1;
        }

        public ReadOnlySpan<T> AsSpan()
        {
            return m_span.Slice(0, m_pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (m_arrayFromPool != null)
            {
                m_pool.Return(m_arrayFromPool);
                m_arrayFromPool = null;
            }
        }

        private void Grow()
        {
            var array = ArrayPool<T>.Shared.Rent(m_span.Length * 2);

            _ = m_span.TryCopyTo(array);

            var toReturn = m_arrayFromPool;
            m_span = m_arrayFromPool = array;
            if (toReturn != null)
            {
                m_pool.Return(toReturn);
            }
        }
    }
}
