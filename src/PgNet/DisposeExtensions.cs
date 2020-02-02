namespace PgNet
{
    using System;

    internal static class DisposeExtensions
    {
        public static void SafeDispose<T>(this T obj) where T : IDisposable
        {
            try
            {
                obj.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}
