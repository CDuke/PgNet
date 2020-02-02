using System;
using PgNet.FrontendMessage;

namespace PgNet.KnownQueries
{
    internal readonly struct BeginTransRepeatableReadQuery : IKnownFrontendMessage
    {
        private static readonly ReadOnlyMemory<byte> s_message = new byte[]
        {
            FrontendMessageCode.Query, sizeof(int) + 37 + 1,
            (byte)'B', (byte)'E', (byte)'G', (byte)'I', (byte)'N', (byte)' ',
            (byte)'I', (byte)'S', (byte)'O', (byte)'L', (byte)'A', (byte)'T', (byte)'I', (byte)'O', (byte)'N', (byte)' ',
            (byte)'L', (byte)'E', (byte)'V', (byte)'E', (byte)'L', (byte)' ',
            (byte)'R', (byte)'E', (byte)'P', (byte)'E', (byte)'A', (byte)'T', (byte)'A', (byte)'B', (byte)'L', (byte)'E', (byte)' ',
            (byte)'R', (byte)'E', (byte)'A', (byte)'D', 0
        };

        public ReadOnlyMemory<byte> GetMessage() => s_message;
    }
}
