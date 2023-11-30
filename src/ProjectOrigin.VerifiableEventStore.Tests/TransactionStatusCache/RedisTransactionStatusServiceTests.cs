using Microsoft.Extensions.Logging;
using Moq;
using ProjectOrigin.TestUtils;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;
using StackExchange.Redis;
using Xunit;

namespace ProjectOrigin.VerifiableEventStore.Tests.TransactionStatusCache;

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
