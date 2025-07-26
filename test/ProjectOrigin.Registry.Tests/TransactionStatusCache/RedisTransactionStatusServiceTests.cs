using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectOrigin.Registry.Extensions;
using ProjectOrigin.Registry.IntegrationTests.Fixtures;
using ProjectOrigin.Registry.Repository.Models;
using ProjectOrigin.Registry.TransactionStatusCache;
using StackExchange.Redis;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace ProjectOrigin.Registry.Tests.TransactionStatusCache;

public class RedisTransactionStatusServiceTests : AbstractTransactionStatusServiceTests, IClassFixture<ReplicatedRedisFixture>
{
    private readonly Mock<ILogger<RedisTransactionStatusService>> _mockLogger;
    private readonly RedisTransactionStatusService _service;
    private readonly IFusionCache _fusionCache;


    public RedisTransactionStatusServiceTests(ReplicatedRedisFixture redisFixture)
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:Type"] = "Redis",
                ["Cache:Redis:ConnectionString"] = redisFixture.MasterConnectionString,
                ["Cache:Redis:Endpoints:0"] = redisFixture.MasterConnectionString,
                ["Cache:Redis:Endpoints:1"] = redisFixture.ReplicaConnectionString,
            })
            .Build();


        services.ConfigureTransactionStatusCache(configuration);
        var serviceProvider = services.BuildServiceProvider();
        _fusionCache = serviceProvider.GetRequiredService<IFusionCache>();

        _mockLogger = new Mock<ILogger<RedisTransactionStatusService>>();
        var connection = serviceProvider.GetRequiredService<IConnectionMultiplexer>();


        _service = new RedisTransactionStatusService(_mockLogger.Object, _fusionCache, _repository, connection);
    }

    protected override ITransactionStatusService Service => _service;
    protected override IInvocationList LoggedMessages => _mockLogger.Invocations;

    [Theory]
    [InlineData(null, null, LogLevel.Error)]
    [InlineData(null, TransactionStatus.Pending, LogLevel.Error)]
    [InlineData(null, TransactionStatus.Unknown, LogLevel.Warning)]
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

        var service = new RedisTransactionStatusService(_mockLogger.Object, _fusionCache, _repository, mockMultiplexer.Object);

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
