using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PgNet.FrontendMessage;

namespace PgNet.KnownQueries
{
    internal readonly struct CommitTransactionQuery : IFrontendMessageSender
    {
        private static readonly ReadOnlyMemory<byte> m_message = new ReadOnlyMemory<byte>(new byte[]
        {
            FrontendMessageCode.Query, sizeof(int) + 6 + 1,
            (byte)'C', (byte)'O', (byte)'M', (byte)'M', (byte)'I', (byte)'T', 0
        });

        public ValueTask<int> Send(Socket socket, CancellationToken cancellationToken)
        {
            return socket.SendAsync(m_message, SocketFlags.None, cancellationToken);
        }
    }
}
