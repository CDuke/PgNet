using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PgNet.IntegrationTests
{
    public class PgConnectorTests
    {
        [Fact]
        public async Task ConnectTest()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            await using var connector = new PgConnector(ArrayPool<byte>.Shared);
            await connector.OpenAsync("localhost", "postgres", "postgres", "admin", CancellationToken.None);
        }
    }
}
