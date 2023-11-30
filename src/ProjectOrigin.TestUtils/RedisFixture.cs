using System;
using System.Threading.Tasks;
using Testcontainers.Redis;
using Xunit;

namespace ProjectOrigin.TestUtils;

public class RedisFixture : IAsyncLifetime
{
    public string HostConnectionString => _container.GetConnectionString();

    public string ContainerConnectionString => new UriBuilder("redis", _container.IpAddress, 6379).Uri.Authority;

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
