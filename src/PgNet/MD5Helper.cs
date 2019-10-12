using System;
using System.Security.Cryptography;
using System.Text;

namespace PgNet
{
    public static class MD5Helper
    {
        private const int MD5HashSize = 16;
        private const int MD5HashHexSize = MD5HashSize * 2;

        private static Encoding s_utf8Encoding = Encoding.UTF8;
        private static byte[] s_md5DeclarationBytes = s_utf8Encoding.GetBytes("md5");

        public const int PasswordHashLength = 3 + MD5HashHexSize; // s_md5DeclarationBytes + MD5HashHexSize

        public static void ComputePassword(string user, string password, ReadOnlySpan<byte> salt, Span<byte> result)
        {
            using (var md5 = MD5.Create())
            {
                var utf8Encoding = s_utf8Encoding;
                var passwordBytesCount = utf8Encoding.GetByteCount(password);
                var userBytesCount = utf8Encoding.GetByteCount(user);
                Span<byte> buffer = stackalloc byte[passwordBytesCount + userBytesCount];
                var writeCount = utf8Encoding.GetBytes(password, buffer);
                utf8Encoding.GetBytes(user, buffer.Slice(writeCount));
                Span<byte> hash = stackalloc byte[MD5HashSize];
                md5.TryComputeHash(buffer, hash, out _);

                var h = new Memory<byte>(new byte[MD5HashHexSize + salt.Length]);
                Span<byte> hexHash = stackalloc byte[MD5HashHexSize + salt.Length];
                HashToString(hash, hexHash.Slice(0, MD5HashHexSize));
                salt.CopyTo(hexHash.Slice(MD5HashHexSize));

                md5.TryComputeHash(hexHash, hash, out _);
                hexHash = hexHash.Slice(0, MD5HashHexSize);
                HashToString(hash, hexHash);

                result[0] = s_md5DeclarationBytes[0];
                result[1] = s_md5DeclarationBytes[1];
                result[2] = s_md5DeclarationBytes[2];
                hexHash.CopyTo(result.Slice(3));
            }
        }

        private static void HashToString(Span<byte> hash, Span<byte> hex)
        {
            var length1 = hex.Length;
            var num1 = 0;
            var index = 0;
            while (index < length1)
            {
                var num2 = hash[num1++];
                hex[index] = GetHexValue(num2 / 16);
                hex[index + 1] = GetHexValue(num2 % 16);
                index += 2;
            }
        }

        private static byte GetHexValue(int i)
        {
            if (i < 10)
                return (byte)(i + 48);
            return (byte)(i + 87);
        }
    }
}
