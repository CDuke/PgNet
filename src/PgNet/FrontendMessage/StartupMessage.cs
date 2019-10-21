using System;
using System.Text;

namespace PgNet.FrontendMessage
{
    internal struct StartupMessage : IFrontendMessageWriter
    {
        private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
        private const int ProtocolVersion3 = 3 << 16; // 196608

        private static readonly byte[] s_databaseParameterName = s_utf8Encoding.GetBytes("database");
        private static readonly byte[] s_userParameterName = s_utf8Encoding.GetBytes("user");
        private static readonly byte[] s_clientEncodingParameterName = s_utf8Encoding.GetBytes("client_encoding");
        private static readonly byte[] s_clientEncodingDefaultParameterValue = s_utf8Encoding.GetBytes("UTF8");
        private static readonly byte[] s_applicationNameParameterName = s_utf8Encoding.GetBytes("application_name");
        private static readonly byte[] s_fallbackApplicationNameParameterName = s_utf8Encoding.GetBytes("fallback_application_name");

        private static readonly byte[] s_searchPathParameterName = s_utf8Encoding.GetBytes("search_path");
        private static readonly byte[] s_timeZoneParameterName = s_utf8Encoding.GetBytes("TimeZone");

        private string m_database;
        private string m_user;
        private string m_applicationName;
        private string m_fallbackApplicationName;
        private string m_searchPath;
        private string m_timeZone;

        public void SetDatabase(string database)
        {
            m_database = database;
        }

        public void SetUser(string user)
        {
            m_user = user;
        }

        public void SetApplicationName(string applicationName)
        {
            m_applicationName = applicationName;
        }

        public void SetFallbackApplicationName(string applicationName)
        {
            m_fallbackApplicationName = applicationName;
        }

        public void SetSearchPath(string searchPath)
        {
            m_searchPath = searchPath;
        }

        public void SetTimeZone(string timeZone)
        {
            m_timeZone = timeZone;
        }

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

        private static void WriteParameter(ref BinarySpanWriter writer, byte[] parameterName, string parameterValue)
        {
            if (parameterValue != null)
            {
                writer.WriteNullTerminateBytes(parameterName);
                writer.WriteNullTerminateString(parameterValue, s_utf8Encoding);
            }
        }

        private static void WriteParameter(ref BinarySpanWriter writer, byte[] parameterName, byte[] parameterValue)
        {
            if (parameterValue != null)
            {
                writer.WriteNullTerminateBytes(parameterName);
                writer.WriteNullTerminateBytes(parameterValue);
            }
        }

        private static int CalculateParameterLength(byte[] parameterName, string parameterValue)
        {
            if (parameterValue != null)
            {
                return parameterName.Length + 1 + s_utf8Encoding.GetByteCount(parameterValue) + 1;
            }

            return 0;
        }

        private static int CalculateParameterLength(byte[] parameterName, byte[] parameterValue)
        {
            if (parameterValue.Length > 0)
            {
                return parameterName.Length + 1 + parameterValue.Length + 1;
            }

            return 0;
        }
    }
}
