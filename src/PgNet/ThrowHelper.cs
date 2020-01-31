using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using PgNet.BackendMessage;

namespace PgNet
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowNotImplementedException()
        {
            throw new NotImplementedException();
        }

        [DoesNotReturn]
        public static void ThrowUnexpectedBackendMessageException(byte messageCode)
        {
            ThrowUnexpectedBackendMessageException((char)messageCode);
        }

        [DoesNotReturn]
        public static void ThrowUnexpectedBackendMessageException(char messageCode)
        {
            throw new UnexpectedBackendMessageException(FormatInvariant("Unexpected backend message '0'", messageCode));
        }

        [DoesNotReturn]
        public static void ThrowUnexpectedAuthenticationRequestType(AuthenticationRequestType authenticationRequestType)
        {
            ThrowPostgresException("Unexpected authentication type '{0}'", (char)authenticationRequestType);
        }

        [DoesNotReturn]
        public static void ThrowPostgresException(string message)
        {
            throw new PostgresException(message);
        }

        [DoesNotReturn]
        public static void ThrowPostgresException<T>(string message, T arg0)
        {
            throw new PostgresException(FormatInvariant(message, arg0));
        }

        private static string FormatInvariant<T>(string message, T arg0)
        {
            return string.Format(CultureInfo.InvariantCulture, message, arg0);
        }
    }
}
