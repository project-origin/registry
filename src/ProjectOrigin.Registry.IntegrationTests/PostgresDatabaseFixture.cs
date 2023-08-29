using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;
using Testcontainers.PostgreSql;
using Xunit;

namespace ProjectOrigin.Electricity.IntegrationTests;

public class PostgresDatabaseFixture : IAsyncLifetime
{
    public string ConnectionString => _postgreSqlContainer.GetConnectionString();

    private PostgreSqlContainer _postgreSqlContainer;

    public PostgresDatabaseFixture()
    {
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        await ResetDatabase();
    }

    public async Task ResetDatabase()
    {
        await _postgreSqlContainer.ExecScriptAsync("DROP SCHEMA public CASCADE;CREATE SCHEMA public;GRANT ALL ON SCHEMA public TO postgres;GRANT ALL ON SCHEMA public TO public;");
        var mockLogger = new Mock<ILogger<PostgresqlUpgrader>>();
        var upgrader = new PostgresqlUpgrader(mockLogger.Object, Options.Create(new PostgresqlEventStoreOptions
        {
            ConnectionString = _postgreSqlContainer.GetConnectionString()
        }));
        upgrader.Upgrade();
    }

    public Task DisposeAsync()
    {
        return _postgreSqlContainer.StopAsync();
    }
}
