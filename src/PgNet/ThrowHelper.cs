using System;
using System.Diagnostics.CodeAnalysis;
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
        public static void ThrowUnexpectedBackendMessageException(char messageCode)
        {
            throw new UnexpectedBackendMessageException($"Unexpected backend message '{messageCode}'");
        }

        [DoesNotReturn]
        public static void ThrowUnexpectedBackendMessageException(byte messageCode)
        {
            ThrowUnexpectedBackendMessageException((char)messageCode);
        }

        [DoesNotReturn]
        public static void ThrowUnexpectedAuthenticationRequestType(AuthenticationRequestType authenticationRequestType)
        {
            ThrowPostgresException($"Unexpected backend message '{(char)authenticationRequestType}'");
        }

        [DoesNotReturn]
        public static void ThrowPostgresException(string message)
        {
            throw new PostgresException(message);
        }
    }
}
