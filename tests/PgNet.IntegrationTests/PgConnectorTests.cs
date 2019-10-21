using System;
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
            var host = Environment.GetEnvironmentVariable("PG_TEST_HOST");
            host ??= "localhost";
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            await using var connector = new PgConnector(ArrayPool<byte>.Shared);
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
        }

        [Fact]
        public async Task QueryTest()
        {
            var host = Environment.GetEnvironmentVariable("PG_TEST_HOST");
            host ??= "localhost";
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            await using var connector = new PgConnector(ArrayPool<byte>.Shared);
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
            await connector.SendQueryAsync("SELECT 1", CancellationToken.None);
        }
    }
}
