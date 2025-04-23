using Microsoft.Extensions.Logging;
using Moq;
using ProjectOrigin.Registry.TransactionStatusCache;
using ProjectOrigin.TestCommon.Fixtures;
using StackExchange.Redis;
using Xunit;

namespace ProjectOrigin.Registry.Tests.TransactionStatusCache;

public class RedisTransactionStatusServiceTests : AbstractTransactionStatusServiceTests, IClassFixture<RedisFixture>
{
    private readonly Mock<ILogger<RedisTransactionStatusService>> _mockLogger;
    private readonly RedisTransactionStatusService _service;

    public RedisTransactionStatusServiceTests(RedisFixture redisFixture)
    {
        var connection = ConnectionMultiplexer.Connect(redisFixture.HostConnectionString);
        _mockLogger = new Mock<ILogger<RedisTransactionStatusService>>();

        _service = new RedisTransactionStatusService(_mockLogger.Object, connection, _repository);
    }

    protected override ITransactionStatusService Service => _service;
    protected override IInvocationList LoggedMessages => _mockLogger.Invocations;
}
