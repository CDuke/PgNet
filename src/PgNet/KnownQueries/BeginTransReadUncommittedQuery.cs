using System;
using PgNet.FrontendMessage;

namespace PgNet.KnownQueries
{
    internal readonly struct BeginTransReadUncommittedQuery : IKnownFrontendMessage
    {
        private static readonly ReadOnlyMemory<byte> s_message = new byte[]
        {
            FrontendMessageCode.Query, sizeof(int) + 50 + 1,
            (byte)'B', (byte)'E', (byte)'G', (byte)'I', (byte)'N', (byte)' ',
            (byte)'T', (byte)'R', (byte)'A', (byte)'N', (byte)'S', (byte)'A', (byte)'C', (byte)'T', (byte)'I', (byte)'O', (byte)'N', (byte)' ',
            (byte)'I', (byte)'S', (byte)'O', (byte)'L', (byte)'A', (byte)'T', (byte)'I', (byte)'O', (byte)'N', (byte)' ',
            (byte)'L', (byte)'E', (byte)'V', (byte)'E', (byte)'L', (byte)' ',
            (byte)'R', (byte)'E', (byte)'A', (byte)'D', (byte)' ',
            (byte)'U', (byte)'N', (byte)'C', (byte)'O', (byte)'M' , (byte)'M', (byte)'I', (byte)'T' , (byte)'T', (byte)'E', (byte)'D',  0
        };

        public ReadOnlyMemory<byte> GetMessage() => s_message;
    }
}
