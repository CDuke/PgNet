using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PgNet.BackendMessage
{
    internal readonly struct SocketReceiver : IReceiver
    {
        private readonly Socket m_socket;

        public SocketReceiver(Socket socket)
        {
            m_socket = socket;
        }

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return m_socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
        }

    }
}
