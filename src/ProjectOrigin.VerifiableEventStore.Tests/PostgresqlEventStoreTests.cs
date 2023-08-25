using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class PostgresqlEventStoreTests : AbstractEventStoreTests<PostgresqlEventStore>, IClassFixture<PostgresDatabaseFixture>, IAsyncLifetime
{
    private readonly PostgresqlEventStore _eventStore;
    private readonly PostgresDatabaseFixture _postgresFixture;

    protected override PostgresqlEventStore EventStore => _eventStore;

    public PostgresqlEventStoreTests(PostgresDatabaseFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;

        var calculator = new BlockSizeCalculator(Options.Create(new VerifiableEventStoreOptions()
        {
            MaxExponent = MaxBatchExponent,
        }));

        var storeOptions = new PostgresqlEventStoreOptions
        {
            ConnectionString = postgresFixture.ConnectionString,
        };
        _eventStore = new PostgresqlEventStore(Options.Create(storeOptions), calculator);
    }

    public Task InitializeAsync()
    {
        return _postgresFixture.ResetDatabase();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
