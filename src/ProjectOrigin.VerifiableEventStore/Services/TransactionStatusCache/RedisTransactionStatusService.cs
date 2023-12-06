using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Repository;
using StackExchange.Redis;

namespace ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

public class RedisTransactionStatusService : ITransactionStatusService
{
    private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(60);

    private readonly ILogger<RedisTransactionStatusService> _logger;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ITransactionRepository _transactionRepository;

    public RedisTransactionStatusService(
        ILogger<RedisTransactionStatusService> logger,
        IConnectionMultiplexer connectionMultiplexer,
        ITransactionRepository transactionRepository)
    {
        _logger = logger;
        _connectionMultiplexer = connectionMultiplexer;
        _transactionRepository = transactionRepository;
    }

    public async Task<TransactionStatusRecord> GetTransactionStatus(TransactionHash transactionHash)
    {
        var cacheStatus = await GetRecordAsync(transactionHash);
        if (cacheStatus is not null)
        {
            return cacheStatus;
        }
        else
        {
            var dbStatus = await _transactionRepository.GetTransactionStatus(transactionHash);
            var statusRecord = new TransactionStatusRecord(dbStatus);
            await SafeSetRecord(transactionHash, statusRecord, null);

            return statusRecord;
        }
    }

    public async Task SetTransactionStatus(TransactionHash transactionHash, TransactionStatusRecord newRecord)
    {
        _logger.LogTrace("Setting transaction status for {transactionHash} to {newStatus}", transactionHash, newRecord.NewStatus);

        var cacheStatus = await GetRecordAsync(transactionHash);

        if (newRecord.NewStatus < cacheStatus?.NewStatus)
        {
            _logger.LogWarning("Transaction {transactionHash} status in cache is {oldStatus} and is higher than {newStatus}, change aborted.", transactionHash, cacheStatus.NewStatus, newRecord.NewStatus);
            return;
        }

        await SafeSetRecord(transactionHash, newRecord, cacheStatus);
    }

    private async Task SafeSetRecord(TransactionHash transactionHash, TransactionStatusRecord newRecord, TransactionStatusRecord? cacheRecord)
    {
        var redisDatabase = _connectionMultiplexer.GetDatabase();

        bool success;
        if (cacheRecord is null)
        {
            success = await redisDatabase.StringSetAsync(
                transactionHash,
                JsonSerializer.Serialize(newRecord),
                when: When.NotExists,
                expiry: CacheTime);
        }
        else
        {
            var transaction = redisDatabase.CreateTransaction();
            transaction.AddCondition(Condition.StringEqual(transactionHash, JsonSerializer.Serialize(cacheRecord)));
            await redisDatabase.StringSetAsync(
                transactionHash,
                JsonSerializer.Serialize(newRecord),
                expiry: CacheTime);
            success = await transaction.ExecuteAsync();
        }

        if (!success)
        {
            _logger.LogWarning("Transaction {transactionHash} status was changed in the cache by another process while trying to set it to {newStatus}, change aborted.", transactionHash, newRecord.NewStatus);
        }
    }

    private async Task<TransactionStatusRecord?> GetRecordAsync(TransactionHash transactionHash)
    {
        var redisDatabase = _connectionMultiplexer.GetDatabase();

        var redisValue = await redisDatabase.StringGetAsync(transactionHash);

        if (redisValue.HasValue)
        {
            await redisDatabase.KeyExpireAsync(transactionHash, CacheTime);
            return JsonSerializer.Deserialize<TransactionStatusRecord>(redisValue!);
        }
        else
            return null;
    }
}
