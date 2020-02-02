using System;

namespace PgNet.FrontendMessage
{
    internal readonly struct PasswordCleartext : IFrontendMessageWriter
    {
        private readonly string m_password;

        public PasswordCleartext(string password) => m_password = password;

        public int CalculateLength()
        {
            return sizeof(byte) + sizeof(int) + PgUtf8.GetByteCount(m_password) + 1;
        }

        public void Write(Memory<byte> buffer)
        {
            var size = buffer.Length - 1;

            var binaryWriter = new BinarySpanWriter(buffer.Span);
            binaryWriter.WriteByte(FrontendMessageCode.Password);
            binaryWriter.WriteInt32(size);
            binaryWriter.WriteNullTerminateUtf8String(m_password);
        }
    }
}
