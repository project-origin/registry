using System;
using System.Text;
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
    private readonly IConnectionMultiplexer _connection;

    public RedisTransactionStatusServiceTests(RedisFixture redisFixture)
    {
        _connection = ConnectionMultiplexer.Connect(redisFixture.HostConnectionString);
        _mockLogger = new Mock<ILogger<RedisTransactionStatusService>>();

        _service = new RedisTransactionStatusService(_mockLogger.Object, _connection, _repository);
    }

    protected override ITransactionStatusService Service => _service;
    protected override IInvocationList LoggedMessages => _mockLogger.Invocations;

    [Theory]
    [InlineData(null, LogLevel.Trace)]
    [InlineData(TransactionStatus.Committed, LogLevel.Warning)]
    [InlineData(TransactionStatus.Finalized, LogLevel.Warning)]
    [InlineData(TransactionStatus.Pending, LogLevel.Error)]
    public async Task SetTransactionStatus_LogsCorrectLevel(
        TransactionStatus? initialStatus,
        LogLevel expectedLevel)
    {
        var txHash = _fixture.Create<TransactionHash>();
        var db = _connection.GetDatabase();
        await SeedKeyAsync(db, txHash, initialStatus);

        if (expectedLevel == LogLevel.Error && initialStatus is null)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(5);
                await SeedKeyAsync(db, txHash, TransactionStatus.Finalized);
            });
        }

        var newRecord = new TransactionStatusRecord(TransactionStatus.Pending);

        await _service.SetTransactionStatus(txHash, newRecord);

        LoggedMessages.Should().Contain(invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            invocation.Arguments[0] is LogLevel &&
            (LogLevel)invocation.Arguments[0] == expectedLevel);
    }

    private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(60);

    private static byte[] Encode(TransactionStatusRecord rec)
    {
        var msg = string.IsNullOrEmpty(rec.Message)
            ? Array.Empty<byte>()
            : Encoding.UTF8.GetBytes(rec.Message);

        var buf = new byte[1 + msg.Length];
        buf[0] = (byte)rec.NewStatus;
        if (msg.Length > 0)
            Buffer.BlockCopy(msg, 0, buf, 1, msg.Length);
        return buf;
    }

    private static async Task SeedKeyAsync(
        IDatabase db,
        TransactionHash key,
        TransactionStatus? status)
    {
        if (status is null)
        {
            await db.KeyDeleteAsync((RedisKey)key);
        }
        else
        {
            var rec = new TransactionStatusRecord(status.Value);
            await db.StringSetAsync(
                (RedisKey)key,
                (RedisValue)Encode(rec),
                expiry: CacheTime,
                when: When.Always);
        }
    }
}

