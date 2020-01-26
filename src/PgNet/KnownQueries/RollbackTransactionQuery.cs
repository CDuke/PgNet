using System;
using PgNet.FrontendMessage;

namespace PgNet.KnownQueries
{
    internal readonly struct RollbackTransactionQuery : IKnownFrontendMessage
    {
        private static readonly ReadOnlyMemory<byte> s_message = new ReadOnlyMemory<byte>(new byte[]
        {
            FrontendMessageCode.Query, sizeof(int) + 8 + 1,
            (byte)'R', (byte)'O', (byte)'L', (byte)'L', (byte)'B', (byte)'A', (byte)'C', (byte)'K', 0
        });

        public ReadOnlyMemory<byte> GetMessage()
        {
            return s_message;
        }
    }
}
