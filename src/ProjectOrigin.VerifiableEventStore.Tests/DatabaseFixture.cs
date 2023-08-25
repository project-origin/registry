using System.Threading.Tasks;
using ProjectOrigin.WalletSystem.Server.Database;
using Testcontainers.PostgreSql;

namespace ProjectOrigin.VerifiableEventStore.Tests;

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
        DatabaseUpgrader.Upgrade(_postgreSqlContainer.GetConnectionString());
    }

    public Task DisposeAsync()
    {
        return _postgreSqlContainer.StopAsync();
    }
}
