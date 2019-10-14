using System;
using System.Text;

namespace PgNet.FrontendMessage
{
    internal readonly struct PasswordCleartext : IFrontendMessageWriter
    {
        private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
        private readonly string m_password;

        public PasswordCleartext(string password)
        {
            m_password = password;
        }

        public int CalculateLength()
        {
            return sizeof(byte) + sizeof(int) + s_utf8Encoding.GetByteCount(m_password) + 1;
        }

        public void Write(Memory<byte> buffer)
        {
            var size = buffer.Length - 1;

            var binaryWriter = new BinarySpanWriter(buffer.Span);
            binaryWriter.WriteByte(FrontendMessageCode.Password);
            binaryWriter.WriteInt32(size);
            binaryWriter.WriteNullTerminateString(m_password, s_utf8Encoding);
        }
    }
}
