using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PgNet.FrontendMessage;

namespace PgNet.KnownQueries
{
    internal readonly struct BeginTransRepeatableReadQuery : IFrontendMessageSender
    {
        private static readonly byte[] m_message =
        {
            FrontendMessageCode.Query, sizeof(int) + 37 + 1,
            (byte)'B', (byte)'E', (byte)'G', (byte)'I', (byte)'N', (byte)' ',
            (byte)'I', (byte)'S', (byte)'O', (byte)'L', (byte)'A', (byte)'T', (byte)'I', (byte)'O', (byte)'N', (byte)' ',
            (byte)'L', (byte)'E', (byte)'V', (byte)'E', (byte)'L', (byte)' ',
            (byte)'R', (byte)'E', (byte)'P', (byte)'E', (byte)'A', (byte)'T', (byte)'A', (byte)'B', (byte)'L', (byte)'E', (byte)' ',
            (byte)'R', (byte)'E', (byte)'A', (byte)'D', 0
        };
        public ValueTask<int> Send(Socket socket, CancellationToken cancellationToken)
        {
            return socket.SendAsync(m_message, SocketFlags.None, cancellationToken);
        }
    }
}
