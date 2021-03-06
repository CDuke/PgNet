using System;
using System.Buffers.Binary;

namespace PgNet
{
    internal ref struct BinarySpanWriter
    {
        private readonly Span<byte> m_span;
        private int m_position;

        public BinarySpanWriter(Span<byte> span)
        {
            m_span = span;
            m_position = 0;
        }

        public void WriteNullTerminateUtf8String(string s)
        {
            var slice = m_span.Slice(m_position);
            var count = PgUtf8.ToUtf8(s, slice);
            slice[count] = 0;
            m_position += count + 1;
        }

        public void WriteNullTerminateUtf8String(ReadOnlyMemory<char> s)
        {
            var slice = m_span.Slice(m_position);
            var count = PgUtf8.ToUtf8(s.Span, slice);
            slice[count] = 0;
            m_position += count + 1;
        }

        public void WriteNullTerminateUtf8String(ReadOnlySpan<char> s)
        {
            var slice = m_span.Slice(m_position);
            var count = PgUtf8.ToUtf8(s, slice);
            slice[count] = 0;
            m_position += count + 1;
        }

        public void WriteBytes(byte[] bytes)
        {
            bytes.CopyTo(m_span.Slice(m_position));
            m_position += bytes.Length;
        }

        public void WriteNullTerminateBytes(byte[] bytes)
        {
            var slice = m_span.Slice(m_position);
            bytes.CopyTo(slice);
            var count = bytes.Length;
            slice[count] = 0;
            m_position += count + 1;
        }

        public void WriteSpan(Span<byte> bytes)
        {
            bytes.CopyTo(m_span.Slice(m_position));
            m_position += bytes.Length;
        }

        public void WriteSpan(ReadOnlySpan<byte> bytes)
        {
            bytes.CopyTo(m_span.Slice(m_position));
            m_position += bytes.Length;
        }

        public void WriteMemory(Memory<byte> bytes)
        {
            bytes.Span.CopyTo(m_span.Slice(m_position));
            m_position += bytes.Length;
        }

        public Span<byte> Span => m_span.Slice(m_position);

        public void Advance(int count) => m_position += count;

        public void WriteByte(byte b)
        {
            m_span[m_position] = b;
            m_position++;
        }

        public void WriteInt32(int i)
        {
            var slice = m_span.Slice(m_position);
            BinaryPrimitives.WriteInt32BigEndian(slice, i);
            m_position += sizeof(int);
        }
    }
}
