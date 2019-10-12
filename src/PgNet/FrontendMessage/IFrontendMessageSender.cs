using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PgNet.FrontendMessage
{
    internal interface IFrontendMessageSender
    {
        ValueTask<int> Send(Socket socket, CancellationToken cancellationToken);
    }
}
