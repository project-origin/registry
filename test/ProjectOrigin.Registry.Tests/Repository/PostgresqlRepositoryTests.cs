using System.Threading.Tasks;
using MsOptions = Microsoft.Extensions.Options.Options;
using ProjectOrigin.Registry.Repository.Postgres;
using ProjectOrigin.TestCommon.Fixtures;
using Xunit;
using ProjectOrigin.ServiceCommon.Database.Postgres;

namespace ProjectOrigin.Registry.Tests.Repository;

public class PostgresqlRepositoryTests : AbstractTransactionRepositoryTests<PostgresqlRepository>, IClassFixture<PostgresDatabaseFixture<Startup>>, IAsyncLifetime
{
    private readonly PostgresqlRepository _transactionRepository;
    private readonly PostgresDatabaseFixture<Startup> _postgresFixture;

    protected override PostgresqlRepository Repository => _transactionRepository;

    public PostgresqlRepositoryTests(PostgresDatabaseFixture<Startup> postgresFixture)
    {
        _postgresFixture = postgresFixture;
        var storeOptions = new PostgresOptions
        {
            ConnectionString = postgresFixture.HostConnectionString,
        };
        _transactionRepository = new PostgresqlRepository(MsOptions.Create(storeOptions));
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
