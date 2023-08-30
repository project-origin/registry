using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;
using Testcontainers.PostgreSql;
using Xunit;

namespace ProjectOrigin.TestUtils;

public class PostgresDatabaseFixture : IAsyncLifetime
{
    public string HostConnectionString => _postgreSqlContainer.GetConnectionString();

    public string NeightborConnectionString
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
