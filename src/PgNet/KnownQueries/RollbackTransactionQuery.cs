using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PgNet.FrontendMessage;

namespace PgNet.KnownQueries
{
    internal readonly struct RollbackTransactionQuery : IFrontendMessageSender
    {
        private static readonly ReadOnlyMemory<byte> m_message = new ReadOnlyMemory<byte>(new byte[]
        {
            FrontendMessageCode.Query, sizeof(int) + 8 + 1,
            (byte)'R', (byte)'O', (byte)'L', (byte)'L', (byte)'B', (byte)'A', (byte)'C', (byte)'K', 0
        });

        public ValueTask<int> Send(Socket socket, CancellationToken cancellationToken)
        {
            return socket.SendAsync(m_message, SocketFlags.None, cancellationToken);
        }
    }
}
