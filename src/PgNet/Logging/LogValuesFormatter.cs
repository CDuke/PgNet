using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PgNet.Logging
{
    /// <summary>
    /// Formatter to convert the named format items like {NamedformatItem} to <see cref="M:string.Format"/> format.
    /// </summary>
    internal class LogValuesFormatter
    {
        private const string NullValue = "(null)";
        private static readonly object[] s_emptyArray = Array.Empty<object>();
        private static readonly char[] s_formatDelimiters = { ',', ':' };
        private readonly string m_format;
        private readonly List<string> m_valueNames = new List<string>();

        public LogValuesFormatter(string format)
        {
            OriginalFormat = format;

            var sb = new StringBuilder();
            var scanIndex = 0;
            var endIndex = format.Length;

            while (scanIndex < endIndex)
            {
                var openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
                var closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);

                if (closeBraceIndex == endIndex)
                {
                    sb.Append(format, scanIndex, endIndex - scanIndex);
                    scanIndex = endIndex;
                }
                else
                {
                    // Format item syntax : { index[,alignment][ :formatString] }.
                    var formatDelimiterIndex = FindIndexOfAny(format, s_formatDelimiters, openBraceIndex, closeBraceIndex);

                    sb.Append(format, scanIndex, openBraceIndex - scanIndex + 1);
                    sb.Append(m_valueNames.Count.ToString(CultureInfo.InvariantCulture));
                    m_valueNames.Add(format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1));
                    sb.Append(format, formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1);

                    scanIndex = closeBraceIndex + 1;
                }
            }

            m_format = sb.ToString();
        }

        public string OriginalFormat { get; }
        public List<string> ValueNames => m_valueNames;

        private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
        {
            // Example: {{prefix{{{Argument}}}suffix}}.
            var braceIndex = endIndex;
            var scanIndex = startIndex;
            var braceOccurenceCount = 0;

            while (scanIndex < endIndex)
            {
                if (braceOccurenceCount > 0 && format[scanIndex] != brace)
                {
                    if (braceOccurenceCount % 2 == 0)
                    {
                        // Even number of '{' or '}' found. Proceed search with next occurence of '{' or '}'.
                        braceOccurenceCount = 0;
                        braceIndex = endIndex;
                    }
                    else
                    {
                        // An unescaped '{' or '}' found.
                        break;
                    }
                }
                else if (format[scanIndex] == brace)
                {
                    if (brace == '}')
                    {
                        if (braceOccurenceCount == 0)
                        {
                            // For '}' pick the first occurence.
                            braceIndex = scanIndex;
                        }
                    }
                    else
                    {
                        // For '{' pick the last occurence.
                        braceIndex = scanIndex;
                    }

                    braceOccurenceCount++;
                }

                scanIndex++;
            }

            return braceIndex;
        }

        private static int FindIndexOfAny(string format, char[] chars, int startIndex, int endIndex)
        {
            var findIndex = format.IndexOfAny(chars, startIndex, endIndex - startIndex);
            return findIndex == -1 ? endIndex : findIndex;
        }

        public string Format(object[] values)
        {
            if (values != null)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = FormatArgument(values[i]);
                }
            }

            return string.Format(CultureInfo.InvariantCulture, m_format, values ?? s_emptyArray);
        }

        internal string Format()
        {
            return m_format;
        }

        internal string Format(object arg0)
        {
            return string.Format(CultureInfo.InvariantCulture, m_format, FormatArgument(arg0));
        }

        internal string Format(object arg0, object arg1)
        {
            return string.Format(CultureInfo.InvariantCulture, m_format, FormatArgument(arg0), FormatArgument(arg1));
        }

        internal string Format(object arg0, object arg1, object arg2)
        {
            return string.Format(CultureInfo.InvariantCulture, m_format, FormatArgument(arg0), FormatArgument(arg1), FormatArgument(arg2));
        }

        public KeyValuePair<string, object> GetValue(object[] values, int index)
        {
            if (index < 0 || index > m_valueNames.Count)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }

            if (m_valueNames.Count > index)
            {
                return new KeyValuePair<string, object>(m_valueNames[index], values[index]);
            }

            return new KeyValuePair<string, object>("{OriginalFormat}", OriginalFormat);
        }

        public IEnumerable<KeyValuePair<string, object>> GetValues(object[] values)
        {
            var valueArray = new KeyValuePair<string, object>[values.Length + 1];
            for (var index = 0; index != m_valueNames.Count; ++index)
            {
                valueArray[index] = new KeyValuePair<string, object>(m_valueNames[index], values[index]);
            }

            valueArray[valueArray.Length - 1] = new KeyValuePair<string, object>("{OriginalFormat}", OriginalFormat);
            return valueArray;
        }

        private static object FormatArgument(object value)
        {
            if (value == null)
            {
                return NullValue;
            }

            // since 'string' implements IEnumerable, special case it
            if (value is string)
            {
                return value;
            }

            // if the value implements IEnumerable, build a comma separated string.
            var enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                return string.Join(", ", enumerable.Cast<object>().Select(o => o ?? NullValue));
            }

            return value;
        }

    }
}
