using System;

namespace PgNet.FrontendMessage
{
    internal readonly struct CancelRequest : IFrontendMessageWriter
    {
        private const int CancelRequestCode = 80877102;
        private readonly int m_processId;
        private readonly int m_secretKey;

        public CancelRequest(int processId, int secretKey)
        {
            m_processId = processId;
            m_secretKey = secretKey;
        }

        public int CalculateLength()
        {
            return sizeof(int) * 4;
        }

        public void Write(Memory<byte> buffer)
        {
            var w = new BinarySpanWriter(buffer.Span);
            w.WriteInt32(16);
            w.WriteInt32(CancelRequestCode);
            w.WriteInt32(m_processId);
            w.WriteInt32(m_secretKey);
        }
    }
}
