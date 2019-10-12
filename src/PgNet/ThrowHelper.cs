using System;
using System.Diagnostics.CodeAnalysis;

namespace PgNet
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowNotImplementedException()
        {
            throw new NotImplementedException();
        }
    }
}
