using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Xunit;

namespace ProjectOrigin.Registry.IntegrationTests.Fixtures;

public sealed class SentinelRedisContainerFixture : IAsyncLifetime
{
    private const string RedisImage = "bitnami/redis:7.2";
    private const string SentinelImage = "bitnami/redis-sentinel:7.2";
    private const int RedisInternalPort = 6379;
    private const int SentinelInternalPort = 26379;

    private readonly INetwork _network;
    private readonly IContainer _redisMaster;
    private readonly IContainer _redisSentinel;

    private readonly string _masterName;

    public SentinelRedisContainerFixture()
    {
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _masterName = $"mymaster-{uniqueId}";
        var networkAlias = $"redis-master-{uniqueId}";
        var sentinelAlias = $"redis-sentinel-{uniqueId}";

        _network = new NetworkBuilder()
            .WithName($"sentinel-net-{uniqueId}")
            .Build();

        _redisMaster = new ContainerBuilder()
            .WithImage(RedisImage)
            .WithName($"redis-master-{uniqueId}")
            .WithNetwork(_network)
            .WithNetworkAliases(networkAlias)
            .WithPortBinding(RedisInternalPort, true)
            .WithEnvironment("ALLOW_EMPTY_PASSWORD", "yes")
            .WithEnvironment("REDIS_REPLICATION_MODE", "master")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(RedisInternalPort))
            .Build();

        _redisSentinel = new ContainerBuilder()
            .WithImage(SentinelImage)
            .WithName($"redis-sentinel-{uniqueId}")
            .WithNetwork(_network)
            .WithNetworkAliases(sentinelAlias)
            .WithPortBinding(SentinelInternalPort, true)
            .WithEnvironment("ALLOW_EMPTY_PASSWORD", "yes")
            .WithEnvironment("REDIS_SENTINEL_DOWN_AFTER_MILLISECONDS", "5000")
            .WithEnvironment("REDIS_MASTER_HOST", networkAlias)
            .WithEnvironment("REDIS_MASTER_PORT_NUMBER", RedisInternalPort.ToString())
            .WithEnvironment("REDIS_MASTER_SET", _masterName)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(SentinelInternalPort))
            .Build();
    }

    public string SentinelHost => _redisSentinel.Hostname;
    public int SentinelMappedPort => _redisSentinel.GetMappedPublicPort(SentinelInternalPort);
    public string SentinelConnectionString => $"{SentinelHost}:{SentinelMappedPort}";
    public string ServiceName => _masterName;

    public string RedisHost => _redisMaster.Hostname;
    public int RedisMappedPort => _redisMaster.GetMappedPublicPort(RedisInternalPort);
    public string RedisDirectConnectionString => $"{RedisHost}:{RedisMappedPort}";

    public async Task InitializeAsync()
    {
        await _network.CreateAsync();
        await _redisMaster.StartAsync();
        await _redisSentinel.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _redisSentinel.DisposeAsync();
        await _redisMaster.DisposeAsync();
        await _network.DeleteAsync();
    }
}
