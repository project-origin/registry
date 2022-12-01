using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace ProjectOrigin.VerifiableEventStore.Tests
{
    public sealed class DatabaseFixture : IDisposable, IAsyncLifetime
    {
        public TestcontainerDatabase Database { get; } = new TestcontainersBuilder<PostgreSqlTestcontainer>()
            .WithDatabase(new PostgreSqlTestcontainerConfiguration
            {
                Database = "db",
                Username = "postgres",
                Password = "postgres",
            })
            .Build();

        public DatabaseFixture()
        {
        }

        public void Dispose()
        {
            Database.DisposeAsync().AsTask().Dispose();
        }

        public Task InitializeAsync() => Database.StartAsync();

        public Task DisposeAsync() => Database.CleanUpAsync();
    }
}
