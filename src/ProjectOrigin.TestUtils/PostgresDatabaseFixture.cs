using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;
using Testcontainers.PostgreSql;
using Xunit;

namespace ProjectOrigin.TestUtils;

public class PostgresDatabaseFixture : IAsyncLifetime
{
    public string HostConnectionString => _postgreSqlContainer.GetConnectionString();

    public string ContainerConnectionString
    {
        get
        {
            var properties = new Dictionary<string, string>
            {
                { "Host", _postgreSqlContainer.IpAddress },
                { "Port", PostgreSqlBuilder.PostgreSqlPort.ToString() },
                { "Database", "postgres" },
                { "Username", "postgres" },
                { "Password", "postgres" }
            };
            return string.Join(";", properties.Select(property => string.Join("=", property.Key, property.Value)));
        }
    }

    private PostgreSqlContainer _postgreSqlContainer;

    public PostgresDatabaseFixture()
    {
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync().ConfigureAwait(false);
        var mockLogger = new Mock<ILogger<PostgresqlUpgrader>>();
        var upgrader = new PostgresqlUpgrader(mockLogger.Object, Options.Create(new PostgresqlEventStoreOptions
        {
            ConnectionString = _postgreSqlContainer.GetConnectionString()
        }));
        upgrader.Upgrade();
    }

    public async Task ResetDatabase()
    {
        var dataSource = NpgsqlDataSource.Create(_postgreSqlContainer.GetConnectionString());
        using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("TRUNCATE blocks, transactions");
    }

    public async Task DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync().ConfigureAwait(false);
    }
}
