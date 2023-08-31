using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ProjectOrigin.TestUtils;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;
using Xunit;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class PostgresqlRepositoryTests : AbstractTransactionRepositoryTests<PostgresqlRepository>, IClassFixture<PostgresDatabaseFixture>, IAsyncLifetime
{
    private readonly PostgresqlRepository _transactionRepository;
    private readonly PostgresDatabaseFixture _postgresFixture;

    protected override PostgresqlRepository Repository => _transactionRepository;

    public PostgresqlRepositoryTests(PostgresDatabaseFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
        var storeOptions = new PostgresqlEventStoreOptions
        {
            ConnectionString = postgresFixture.HostConnectionString,
        };
        _transactionRepository = new PostgresqlRepository(Options.Create(storeOptions));
    }

    public async Task InitializeAsync()
    {
        await _postgresFixture.ResetDatabase();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
