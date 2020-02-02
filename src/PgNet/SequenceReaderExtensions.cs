using System;
using System.Buffers;
using System.Text;

namespace PgNet
{
    internal static class SequenceReaderExtensions
    {
        public static string ReadNullTerminateString(this ref SequenceReader<byte> reader, Encoding encoding)
        {
            var remaining = reader.UnreadSpan;
            var index = remaining.IndexOf((byte)0);
            var result = string.Empty;
            if (index != -1)
            {
                result = encoding.GetString(remaining.Slice(0, index));
                reader.Advance(index + 1);
            }

            return result;
        }

        public static string ReadUtf8NullTerminateStringAsUtf16(this ref SequenceReader<byte> reader)
        {
            var remaining = reader.UnreadSpan;
            var index = remaining.IndexOf((byte)0);
            var result = string.Empty;
            if (index != -1)
            {
                result = PgUtf8.ToUtf16(remaining.Slice(0, index));
                reader.Advance(index + 1);
            }

            return result;
        }
    }
}
