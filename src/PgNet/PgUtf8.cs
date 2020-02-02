using System.Text;
using System.Text.Unicode;

namespace PgNet
{
    using System;

    internal static class PgUtf8
    {
        private static readonly Encoding s_utf8 = Encoding.UTF8;

        public static int GetByteCount(string s) => s_utf8.GetByteCount(s);
        public static int GetByteCount(ReadOnlySpan<char> s) => s_utf8.GetByteCount(s);

        public static int ToUtf8(string s, Span<byte> buffer)
        {
            _ = Utf8.FromUtf16(s, buffer, out _, out var count);
            return count;
        }

        public static int ToUtf8(ReadOnlySpan<char> s, Span<byte> buffer)
        {
            _ = Utf8.FromUtf16(s, buffer, out _, out var count);
            return count;
        }

        public static string ToUtf16(ReadOnlySpan<byte> source)
        {
            return s_utf8.GetString(source);
        }
    }
}
