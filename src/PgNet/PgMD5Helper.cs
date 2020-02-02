using System;

namespace PgNet
{
    internal static class PgMD5Helper
    {
        private const int MD5HashSize = MD5.MD5HashByteSize;
        private const int MD5HashHexSize = MD5.MD5HashHexByteSize;

        private static ReadOnlySpan<byte> s_md5DeclarationBytes => new [] { (byte)'m', (byte)'d', (byte)'5' };

        public const int PasswordHashLength = 3 + MD5HashHexSize; // s_md5DeclarationBytes + MD5HashHexSize

        public static void ComputePassword(string user, string password, ReadOnlySpan<byte> salt, Span<byte> result)
        {
            var passwordBytesCount = PgUtf8.GetByteCount(password);
            var userBytesCount = PgUtf8.GetByteCount(user);

            var tempSize = passwordBytesCount + userBytesCount + MD5HashSize + MD5HashHexSize + salt.Length;
            Span<byte> temp = stackalloc byte[tempSize];

            var buffer = temp.Slice(0, passwordBytesCount + userBytesCount);
            var writeCount = PgUtf8.ToUtf8(password, buffer);
            _ = PgUtf8.ToUtf8(user, buffer.Slice(writeCount));
            var hash = temp.Slice(buffer.Length, MD5HashSize);
            MD5.Instance.TryComputeHash(buffer, hash);

            var hexHash = temp.Slice(buffer.Length + hash.Length);
            HashToString(hash, hexHash.Slice(0, MD5HashHexSize));
            salt.CopyTo(hexHash.Slice(MD5HashHexSize));

            MD5.Instance.TryComputeHash(hexHash, hash);
            hexHash = hexHash.Slice(0, MD5HashHexSize);
            HashToString(hash, hexHash);

            s_md5DeclarationBytes.CopyTo(result);

            hexHash.CopyTo(result.Slice(3));
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
