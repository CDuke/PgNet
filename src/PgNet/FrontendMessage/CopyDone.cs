using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PgNet.FrontendMessage
{
    internal readonly struct CopyDone
    {
        private static readonly ReadOnlyMemory<byte> s_message = new ReadOnlyMemory<byte>(new[]
        {
            FrontendMessageCode.CopyDone, (byte)0, (byte)0, (byte)0, (byte)4
        });

        public ValueTask<int> Send(Socket socket, CancellationToken cancellationToken)
        {
            return socket.SendAsync(s_message, SocketFlags.None, cancellationToken);
        }
    }
}
