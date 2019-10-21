using System;
using System.Text;

namespace PgNet.FrontendMessage
{
    internal struct Query : IFrontendMessageWriter
    {
        private readonly ReadOnlyMemory<char> m_query;

        public Query(ReadOnlyMemory<char> query)
        {
            m_query = query;
        }

        public int CalculateLength()
        {
            return 1 + sizeof(int) + Encoding.UTF8.GetByteCount(m_query.Span) + 1;
        }

        public void Write(Memory<byte> buffer)
        {
            var w = new BinarySpanWriter(buffer.Span);
            w.WriteByte(FrontendMessageCode.Query);
            w.WriteInt32(buffer.Length - 1);
            w.WriteNullTerminateString(m_query, Encoding.UTF8);
        }
    }
}
