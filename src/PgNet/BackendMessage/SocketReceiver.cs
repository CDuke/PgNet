using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PgNet.BackendMessage
{
    internal readonly struct SocketReceiver : IReceiver
    {
        private readonly Socket _socket;

        public SocketReceiver(Socket socket)
        {
            _socket = socket;
        }

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
        }
    }
}