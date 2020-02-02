using System;
using PgNet.FrontendMessage;

namespace PgNet.KnownQueries
{
    internal readonly struct BeginTransSerializableQuery : IKnownFrontendMessage
    {
        private static readonly ReadOnlyMemory<byte> s_message = new byte[]
        {
            FrontendMessageCode.Query, sizeof(int) + 46 + 1,
            (byte)'B', (byte)'E', (byte)'G', (byte)'I', (byte)'N', (byte)' ',
            (byte)'T', (byte)'R', (byte)'A', (byte)'N', (byte)'S', (byte)'A', (byte)'C', (byte)'T', (byte)'I', (byte)'O', (byte)'N', (byte)' ',
            (byte)'I', (byte)'S', (byte)'O', (byte)'L', (byte)'A', (byte)'T', (byte)'I', (byte)'O', (byte)'N', (byte)' ',
            (byte)'L', (byte)'E', (byte)'V', (byte)'E', (byte)'L', (byte)' ',
            (byte)'S', (byte)'E', (byte)'R', (byte)'I', (byte)'A', (byte)'L', (byte)'I', (byte)'Z' , (byte)'A', (byte)'B', (byte)'L' , (byte)'E', 0
        };

        public ReadOnlyMemory<byte> GetMessage() => s_message;
    }
}
