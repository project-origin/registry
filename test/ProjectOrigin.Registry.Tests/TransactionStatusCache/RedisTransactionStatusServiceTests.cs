
using System;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectOrigin.Registry.Repository.Models;
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

    [Theory]
    [InlineData(null, null, LogLevel.Error)]
    [InlineData(null, TransactionStatus.Unknown, LogLevel.Warning)]
    [InlineData(null, TransactionStatus.Pending, LogLevel.Error)]
    [InlineData(TransactionStatus.Unknown, TransactionStatus.Unknown, LogLevel.Error)]
    public async Task SetTransactionStatus_LogsCorrectMessage_RaceCondition(TransactionStatus? firstReturn, TransactionStatus? secondReturn, LogLevel logLevel)
    {
        // Arrange
        var newRecord = new TransactionStatusRecord(TransactionStatus.Pending);

        var startState = firstReturn.HasValue ? JsonSerializer.Serialize(new TransactionStatusRecord(firstReturn.Value)) : string.Empty;
        var secondState = secondReturn.HasValue ? JsonSerializer.Serialize(new TransactionStatusRecord(secondReturn.Value)) : string.Empty;

        var transactionHash = _fixture.Create<TransactionHash>();
        var mockMultiplexer = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        var transaction = new Mock<ITransaction>();
        mockMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);

        mockDatabase
            .SetupSequence(db => db.CreateTransaction(It.IsAny<object>()))
            .Returns(transaction.Object);
        transaction
            .Setup(t => t.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);
        mockDatabase
            .SetupSequence(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var service = new RedisTransactionStatusService(_mockLogger.Object, mockMultiplexer.Object, _repository);

        mockDatabase
            .SetupSequence(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(startState))
            .ReturnsAsync(new RedisValue(secondState));

        // Act
        await service.SetTransactionStatus(transactionHash, newRecord);

        // Assert
        LoggedMessages
            .Should()
            .Contain(x => x.Method.Name == nameof(ILogger.Log) && x.Arguments[0].Equals(logLevel));
    }
}
