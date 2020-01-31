using System;
using System.Threading;
using System.Threading.Tasks;

namespace PgNet.BackendMessage
{
    internal interface IReceiver
    {
        ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    }
}