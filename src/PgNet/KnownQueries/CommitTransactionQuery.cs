using System;
using PgNet.FrontendMessage;

namespace PgNet.KnownQueries
{
    internal readonly struct CommitTransactionQuery : IKnownFrontendMessage
    {
        private static readonly ReadOnlyMemory<byte> s_message = new ReadOnlyMemory<byte>(new byte[]
        {
            FrontendMessageCode.Query, sizeof(int) + 6 + 1,
            (byte)'C', (byte)'O', (byte)'M', (byte)'M', (byte)'I', (byte)'T', 0
        });
        public ReadOnlyMemory<byte> GetMessage()
        {
            return s_message;
        }
    }
}
