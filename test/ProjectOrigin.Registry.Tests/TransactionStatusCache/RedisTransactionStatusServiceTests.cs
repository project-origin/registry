using Microsoft.Extensions.Logging;
using Moq;
using ProjectOrigin.Registry.TransactionStatusCache;
using ProjectOrigin.TestCommon.Fixtures;
using StackExchange.Redis;
using Xunit;

namespace ProjectOrigin.Registry.Tests.TransactionStatusCache;

public class RedisTransactionStatusServiceTests : AbstractTransactionStatusServiceTests, IClassFixture<RedisFixture>
{
    private RedisTransactionStatusService _service;

    public RedisTransactionStatusServiceTests(RedisFixture redisFixture)
    {
        var connection = ConnectionMultiplexer.Connect(redisFixture.HostConnectionString);
        var logger = new Mock<ILogger<RedisTransactionStatusService>>();
        _service = new RedisTransactionStatusService(logger.Object, connection, _repository);
    }

    protected override ITransactionStatusService Service => _service;
}
