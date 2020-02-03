using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace PgNet.Logging
{
    /// <summary>
    /// LogValues to enable formatting options supported by <see cref="M:string.Format"/>.
    /// This also enables using {NamedformatItem} in the format string.
    /// </summary>
    internal readonly struct FormattedLogValues : IReadOnlyList<KeyValuePair<string, object>>
    {
        internal const int MaxCachedFormatters = 1024;
        private const string NullFormat = "[null]";
        private static int s_count;
        private static readonly ConcurrentDictionary<string, LogValuesFormatter> s_formatters = new ConcurrentDictionary<string, LogValuesFormatter>();
        private readonly LogValuesFormatter? m_formatter;
        private readonly object?[] m_values;
        private readonly string m_originalMessage;

        public FormattedLogValues(string format, params object[] values)
        {
            if (values != null && values.Length != 0 && format != null)
            {
                if (s_count >= MaxCachedFormatters)
                {
                    if (!s_formatters.TryGetValue(format, out m_formatter))
                    {
                        m_formatter = new LogValuesFormatter(format);
                    }
                }
                else
                {
                    m_formatter = s_formatters.GetOrAdd(format, f =>
                    {
                        Interlocked.Increment(ref s_count);
                        return new LogValuesFormatter(f);
                    });
                }
            }
            else
            {
                m_formatter = null;
            }

            m_originalMessage = format ?? NullFormat;
            m_values = values;
        }

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (index == Count - 1)
                {
                    return new KeyValuePair<string, object>("{OriginalFormat}", m_originalMessage);
                }

                return m_formatter.GetValue(m_values, index);
            }
        }

        public int Count
        {
            get
            {
                if (m_formatter == null)
                {
                    return 1;
                }

                return m_formatter.ValueNames.Count + 1;
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            for (var i = 0; i < Count; ++i)
            {
                yield return this[i];
            }
        }

        public override string ToString()
        {
            if (m_formatter == null)
            {
                return m_originalMessage;
            }

            return m_formatter.Format(m_values);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
