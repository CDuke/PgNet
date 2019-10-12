using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PgNet.FrontendMessage
{
    internal readonly struct Flush : IFrontendMessageSender
    {
        private static readonly Memory<byte> s_message = new Memory<byte>(new[]
        {
            FrontendMessageCode.Flush, (byte)0, (byte)0, (byte)0, (byte)4
        });

        public ValueTask<int> Send(Socket socket, CancellationToken cancellationToken)
        {
            return socket.SendAsync(s_message, SocketFlags.None, cancellationToken);
        }
    }
}
