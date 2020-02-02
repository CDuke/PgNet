using System;

namespace PgNet.FrontendMessage
{
    internal struct StartupMessage : IFrontendMessageWriter
    {
        private const int ProtocolVersion3 = 3 << 16; // 196608

        private static ReadOnlySpan<byte> s_databaseParameterName => new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)'b', (byte)'a', (byte)'s', (byte)'e', (byte)0 };
        private static ReadOnlySpan<byte> s_userParameterName => new[] { (byte)'u', (byte)'s', (byte)'e', (byte)'r', (byte)0 };
        private static ReadOnlySpan<byte> s_clientEncodingParameterName
            => new[] { (byte)'c', (byte)'l', (byte)'i', (byte)'e', (byte)'n', (byte)'t', (byte)'_', (byte)'e', (byte)'n', (byte)'c', (byte)'o', (byte)'d', (byte)'i', (byte)'n', (byte)'g', (byte)0 };
        private static ReadOnlySpan<byte> s_clientEncodingDefaultParameterValue => new[] { (byte)'U', (byte)'T', (byte)'F', (byte)'8', (byte)0 };
        private static ReadOnlySpan<byte> s_applicationNameParameterName
            => new[] { (byte)'a', (byte)'p', (byte)'p', (byte)'l', (byte)'i', (byte)'c', (byte)'a', (byte)'t', (byte)'i', (byte)'o', (byte)'n', (byte)'_', (byte)'n', (byte)'a', (byte)'m', (byte)'e', (byte)0 };

        private static ReadOnlySpan<byte> s_fallbackApplicationNameParameterName
            => new[]
            {
                (byte)'f', (byte)'a', (byte)'l', (byte)'l', (byte)'b', (byte)'a', (byte)'c', (byte)'k', (byte)'_',
                (byte)'a', (byte)'p', (byte)'p', (byte)'l', (byte)'i', (byte)'c', (byte)'a', (byte)'t', (byte)'i', (byte)'o', (byte)'n', (byte)'_', (byte)'n', (byte)'a', (byte)'m', (byte)'e', (byte)0
            };

        private static ReadOnlySpan<byte> s_searchPathParameterName
            => new[] { (byte)'s', (byte)'e', (byte)'a', (byte)'r', (byte)'c', (byte)'h', (byte)'_', (byte)'p', (byte)'a', (byte)'t', (byte)'h', (byte)0 };

        private static ReadOnlySpan<byte> s_timeZoneParameterName
            => new[] { (byte)'T', (byte)'i', (byte)'m', (byte)'e', (byte)'Z', (byte)'o', (byte)'n', (byte)'e', (byte)0 };

        private string m_database;
        private string m_user;
        private string m_applicationName;
        private string m_fallbackApplicationName;
        private string m_searchPath;
        private string m_timeZone;

        public void SetDatabase(string database) => m_database = database;

        public void SetUser(string user) => m_user = user;

        public void SetApplicationName(string applicationName) => m_applicationName = applicationName;

        public void SetFallbackApplicationName(string applicationName) => m_fallbackApplicationName = applicationName;

        public void SetSearchPath(string searchPath) => m_searchPath = searchPath;

        public void SetTimeZone(string timeZone) => m_timeZone = timeZone;

        public int CalculateLength()
        {
            var len = sizeof(int) +  // Length
                     sizeof(int) +  // Protocol version
                     sizeof(byte);  // Trailing zero byte

            len += CalculateParameterLength(s_databaseParameterName, m_database);
            len += CalculateParameterLength(s_userParameterName, m_user);
            len += CalculateParameterLength(s_clientEncodingParameterName, s_clientEncodingDefaultParameterValue);

            len += CalculateParameterLength(s_applicationNameParameterName, m_applicationName);
            len += CalculateParameterLength(s_fallbackApplicationNameParameterName, m_fallbackApplicationName);
            len += CalculateParameterLength(s_searchPathParameterName, m_searchPath );
            len += CalculateParameterLength(s_timeZoneParameterName, m_timeZone);
            
            return len;
        }

        public void Write(Memory<byte> buffer)
        {
            var w = new BinarySpanWriter(buffer.Span);
            var size = buffer.Length;
            w.WriteInt32(size);
            w.WriteInt32(ProtocolVersion3);

            WriteParameter(ref w, s_databaseParameterName, m_database);
            WriteParameter(ref w, s_userParameterName, m_user);
            WriteParameter(ref w, s_clientEncodingParameterName, s_clientEncodingDefaultParameterValue);

            WriteParameter(ref w, s_applicationNameParameterName, m_applicationName);
            WriteParameter(ref w, s_fallbackApplicationNameParameterName, m_fallbackApplicationName);
            WriteParameter(ref w, s_searchPathParameterName, m_searchPath);
            WriteParameter(ref w, s_timeZoneParameterName, m_timeZone);

            w.WriteByte(0);
        }

        private static void WriteParameter(ref BinarySpanWriter writer, ReadOnlySpan<byte> parameterName, string parameterValue)
        {
            if (parameterValue != null)
            {
                writer.WriteSpan(parameterName);
                writer.WriteNullTerminateUtf8String(parameterValue);
            }
        }

        private static void WriteParameter(ref BinarySpanWriter writer, ReadOnlySpan<byte> parameterName, ReadOnlySpan<byte> parameterValue)
        {
            if (parameterValue != null)
            {
                writer.WriteSpan(parameterName);
                writer.WriteSpan(parameterValue);
            }
        }

        private static int CalculateParameterLength(ReadOnlySpan<byte> parameterName, string parameterValue)
        {
            if (parameterValue != null)
            {
                return parameterName.Length + PgUtf8.GetByteCount(parameterValue) + 1;
            }

            return 0;
        }

        private static int CalculateParameterLength(ReadOnlySpan<byte> parameterName, ReadOnlySpan<byte> parameterValue)
        {
            if (parameterValue.Length > 0)
            {
                return parameterName.Length + parameterValue.Length;
            }

            return 0;
        }
    }
}
