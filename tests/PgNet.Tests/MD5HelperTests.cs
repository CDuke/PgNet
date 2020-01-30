using System;
using System.Text;
using Xunit;

namespace PgNet.Tests
{
    public class MD5HelperTests
    {
        [Fact]
        public void Test1()
        {
            Span<byte> h = stackalloc byte[PgMD5Helper.PasswordHashLength];
            var salt = new byte[] {1, 2, 3, 4};
            PgMD5Helper.ComputePassword("postgres", "admin", salt, h);
            var h2 = CalcHash("postgres", "admin", salt);

            for (var i = 0; i < h.Length; i++)
            {
                if (h[i] != h2[i])
                    throw new Exception();
            }
        }

        private static byte[] CalcHash(string userName, string passwd, byte[] salt)
        {
            byte[] result;
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                // First phase
                var passwordBytes = Encoding.UTF8.GetBytes(passwd);
                var usernameBytes = Encoding.UTF8.GetBytes(userName);
                var cryptBuf = new byte[passwordBytes.Length + usernameBytes.Length];
                passwordBytes.CopyTo(cryptBuf, 0);
                usernameBytes.CopyTo(cryptBuf, passwordBytes.Length);

                var sb = new StringBuilder();
                var hashResult = md5.ComputeHash(cryptBuf);
                foreach (var b in hashResult)
                    sb.Append(b.ToString("x2"));

                var prehash = sb.ToString();

                var prehashbytes = Encoding.UTF8.GetBytes(prehash);
                cryptBuf = new byte[prehashbytes.Length + 4];

                Array.Copy(salt, 0, cryptBuf, prehashbytes.Length, 4);

                // 2.
                prehashbytes.CopyTo(cryptBuf, 0);

                sb = new StringBuilder("md5");
                hashResult = md5.ComputeHash(cryptBuf);
                foreach (var b in hashResult)
                    sb.Append(b.ToString("x2"));

                var resultString = sb.ToString();
                result = new byte[Encoding.UTF8.GetByteCount(resultString) + 1];
                Encoding.UTF8.GetBytes(resultString, 0, resultString.Length, result, 0);
                result[^1] = 0;
            }

            return result;
        }
    }
}
