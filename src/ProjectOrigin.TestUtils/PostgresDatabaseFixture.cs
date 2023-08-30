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
        await _postgreSqlContainer.StartAsync().ConfigureAwait(false);
        await Task.Delay(5000);
        await ResetDatabase().ConfigureAwait(false);
    }

    public async Task ResetDatabase()
    {
        try
        {
            await _postgreSqlContainer.ExecScriptAsync("DROP SCHEMA public CASCADE;CREATE SCHEMA public;GRANT ALL ON SCHEMA public TO postgres;GRANT ALL ON SCHEMA public TO public;").ConfigureAwait(false);
            var mockLogger = new Mock<ILogger<PostgresqlUpgrader>>();
            var upgrader = new PostgresqlUpgrader(mockLogger.Object, Options.Create(new PostgresqlEventStoreOptions
            {
                ConnectionString = _postgreSqlContainer.GetConnectionString()
            }));
            upgrader.Upgrade();
        }
        catch (Exception ex)
        {
            var log = await _postgreSqlContainer.GetLogsAsync();
            Console.WriteLine($"Failed to reset database \n {ex.Message} \n\n {ex.StackTrace} \n\nContainerStatus {_postgreSqlContainer.State} \n-----------stdout---------\n {log.Stdout}\n----------stderr------\n {log.Stderr}\n--------------\n");
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync().ConfigureAwait(false);
    }
}
