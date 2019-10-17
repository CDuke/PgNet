using System;

namespace PgNet.FrontendMessage
{
    internal readonly struct PasswordMD5Message : IFrontendMessageWriter
    {
        private readonly string m_user;
        private readonly string m_password;
        private readonly ReadOnlyMemory<byte> m_salt;

        public PasswordMD5Message(string user, string password, ReadOnlyMemory<byte> salt)
        {
            m_user = user;
            m_password = password;
            m_salt = salt;
        }
        public int CalculateLength()
        {
            return sizeof(byte) + sizeof(int) + PgMD5Helper.PasswordHashLength + 1;
        }

        public void Write(Memory<byte> buffer)
        {
            Span<byte> hash = stackalloc byte[PgMD5Helper.PasswordHashLength];
            PgMD5Helper.ComputePassword(m_user, m_password, m_salt.Span, hash);

            var binaryWriter = new BinarySpanWriter(buffer.Span);
            binaryWriter.WriteByte(FrontendMessageCode.Password);
            binaryWriter.WriteInt32(sizeof(int) + PgMD5Helper.PasswordHashLength + 1);
            hash.CopyTo(binaryWriter.Span);
            binaryWriter.Advance(PgMD5Helper.PasswordHashLength);
            binaryWriter.WriteByte(0);
        }
    }
}
