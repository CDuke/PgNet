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
        public async Task BeginTransRepeatableReadTest()
        {
            var host = GetHost();
            await using var connector = CreatePgConnector();
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
            await connector.SendQueryAsync("BEGIN ISOLATION LEVEL REPEATABLE READ", CancellationToken.None);
        }

        [Fact]
        public async Task BeginTransSerializableTest()
        {
            var host = GetHost();
            await using var connector = CreatePgConnector();
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
            await connector.SendQueryAsync("BEGIN TRANSACTION ISOLATION LEVEL SERIALIZABLE", CancellationToken.None);
        }

        [Fact]
        public async Task BeginTransReadCommittedTest()
        {
            var host = GetHost();
            await using var connector = CreatePgConnector();
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
            await connector.SendQueryAsync("BEGIN TRANSACTION ISOLATION LEVEL READ COMMITTED", CancellationToken.None);
        }

        [Fact]
        public async Task BeginTransReadUncommittedTest()
        {
            var host = GetHost();
            await using var connector = CreatePgConnector();
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
            await connector.SendQueryAsync("BEGIN TRANSACTION ISOLATION LEVEL READ UNCOMMITTED", CancellationToken.None);
        }

        [Fact]
        public async Task CommitWithTransactionTest()
        {
            var host = GetHost();
            await using var connector = CreatePgConnector();
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
            await connector.SendQueryAsync("BEGIN ISOLATION LEVEL REPEATABLE READ", CancellationToken.None);
            await connector.SendQueryAsync("COMMIT", CancellationToken.None);
        }

        [Fact]
        public async Task CommitWithNoTransactionTest()
        {
            var host = GetHost();
            await using var connector = CreatePgConnector();
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
            await connector.SendQueryAsync("COMMIT", CancellationToken.None);
        }

        [Fact]
        public async Task RollbackWithTransactionTest()
        {
            var host = GetHost();
            await using var connector = CreatePgConnector();
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
            await connector.SendQueryAsync("BEGIN ISOLATION LEVEL REPEATABLE READ", CancellationToken.None);
            await connector.SendQueryAsync("ROLLBACK", CancellationToken.None);
        }

        [Fact]
        public async Task RollbackWithNoTransactionTest()
        {
            var host = GetHost();
            await using var connector = CreatePgConnector();
            await connector.OpenAsync(host, "postgres", "postgres", "admin", CancellationToken.None);
            await connector.SendQueryAsync("ROLLBACK", CancellationToken.None);
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
