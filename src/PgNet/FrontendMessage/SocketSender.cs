using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PgNet.FrontendMessage
{
    internal readonly struct SocketSender : ISender
    {
        private readonly Socket m_socket;

        public SocketSender(Socket socket) => m_socket = socket;

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) =>
            m_socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
    }
}
