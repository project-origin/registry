using System.Threading.Tasks;
using Testcontainers.Redis;
using Xunit;

namespace ProjectOrigin.TestUtils;

public class RedisFixture : IAsyncLifetime
{
    public string GetConnectionString() => _container.GetConnectionString();

    private RedisContainer _container;

    public RedisFixture()
    {
        _container = new RedisBuilder()
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}
