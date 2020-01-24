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
        public PgConnectorTests()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [Fact]
        public async Task ConnectTest()
        {
            var host = GetHost();
            await using var connector = CreatePgConnector();
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
        }

        [Fact]
        public async Task QueryTest()
        {
            var host = GetHost();
            await using var connector = CreatePgConnector();
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
            await connector.SendQueryAsync("SELECT 1", CancellationToken.None);
        }

        [Fact]
        public async Task CancelTest()
        {
            var host = GetHost();
            await using var connector = CreatePgConnector();
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
            await connector.Cancel(CancellationToken.None);
        }

        private static string GetHost()
        {
            var host = Environment.GetEnvironmentVariable("PG_TEST_HOST");
            host ??= "localhost";
            return host;
        }

        private static PgConnector CreatePgConnector()
        {
            return new PgConnector(ArrayPool<byte>.Shared);
        }
    }
}
