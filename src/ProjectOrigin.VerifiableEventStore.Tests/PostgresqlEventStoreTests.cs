using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ProjectOrigin.TestUtils;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;
using Xunit;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class PostgresqlEventStoreTests : AbstractEventStoreTests<PostgresqlRepository>, IClassFixture<PostgresDatabaseFixture>, IAsyncLifetime
{
    private readonly PostgresqlRepository _eventStore;
    private readonly PostgresDatabaseFixture _postgresFixture;

    protected override PostgresqlRepository Repository => _eventStore;

    public PostgresqlEventStoreTests(PostgresDatabaseFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
        var storeOptions = new PostgresqlEventStoreOptions
        {
            ConnectionString = postgresFixture.HostConnectionString,
        };
        _eventStore = new PostgresqlRepository(Options.Create(storeOptions));
    }

    public Task InitializeAsync()
    {
        Console.WriteLine($"Initializing database test. {DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff")}");
        return _postgresFixture.ResetDatabase();

    }

    public Task DisposeAsync()
    {
        Console.WriteLine($"Disposing database test. {DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff")}");
        return Task.CompletedTask;
    }
}
