using System;
using System.Threading;
using System.Threading.Tasks;

namespace PgNet.FrontendMessage
{
    internal interface ISender
    {
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
    }
}
