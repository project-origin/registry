using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using ProjectOrigin.TestCommon.Extensions;
using Testcontainers.Redis;
using Xunit;

namespace ProjectOrigin.Registry.Tests.Fixtures;

public class ReplicatedRedisFixture : IAsyncLifetime
{
    private readonly INetwork _network;
    private readonly RedisContainer _containerMaster;
    private readonly RedisContainer _containerReplica;

    public string MasterConnectionString => _containerMaster.GetConnectionString();
    public string ReplicaConnectionString => _containerReplica.GetConnectionString();

    public ReplicatedRedisFixture()
    {
        _network = new NetworkBuilder()
            .WithName("redis-network")
            .Build();

        _containerMaster = new RedisBuilder()
            .WithName("redis-master")
            .WithNetwork("redis-network")
            .WithNetworkAliases("redis-master")
            .WithExposedPort(6379)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();

        _containerReplica = new RedisBuilder()
            .WithName("redis-replica")
            .WithNetwork("redis-network")
            .WithNetworkAliases("redis-replica")
            .WithExposedPort(6379)
            .WithCommand("redis-server", "--slaveof", "redis-master", "6379")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _network.CreateAsync();
        await _containerMaster.StartWithLoggingAsync();
        await _containerReplica.StartWithLoggingAsync();
    }

    public async Task DisposeAsync()
    {
        await _containerReplica.StopAsync();
        await _containerMaster.StopAsync();
        await _network.DisposeAsync();
    }
}
