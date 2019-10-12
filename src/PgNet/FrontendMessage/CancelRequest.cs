using System;

namespace PgNet.FrontendMessage
{
    internal struct CancelRequest
    {
        private int m_processId;
        private int m_secretKey;

        public void SetProcessId(int processId)
        {
            m_processId = processId;
        }

        public void SetSecretKey(int secretKey)
        {
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
            w.WriteInt32(80877102);
            w.WriteInt32(m_processId);
            w.WriteInt32(m_secretKey);
        }
    }
}
