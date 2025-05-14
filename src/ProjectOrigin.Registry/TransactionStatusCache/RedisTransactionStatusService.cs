using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.Repository;
using ProjectOrigin.Registry.Repository.Models;
using StackExchange.Redis;

namespace ProjectOrigin.Registry.TransactionStatusCache;

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

        if (cacheRecord is null)
        {
            var success = await redisDatabase.StringSetAsync(
                transactionHash,
                JsonSerializer.Serialize(newRecord),
                when: When.NotExists,
                expiry: CacheTime);

            if (!success)
            {
                var cacheStatus = await GetRecordAsync(transactionHash);
                if (cacheStatus!.NewStatus == TransactionStatus.Unknown)
                {
                    _logger.LogWarning("Transaction {transactionHash} status was set to unknown while setting it to {newStatus}, retrying.", transactionHash, newRecord.NewStatus);
                    await SafeSetRecord(transactionHash, newRecord, cacheRecord);
                }
                else
                {
                    _logger.LogWarning("Transaction {transactionHash} status was set in the cache by another process while trying to set it to {newStatus}, change aborted.", transactionHash, newRecord.NewStatus);
                }
            }
        }
        else
        {
            var transaction = redisDatabase.CreateTransaction();
            transaction.AddCondition(Condition.StringEqual(transactionHash, JsonSerializer.Serialize(cacheRecord)));
            _ = transaction.StringSetAsync(
                transactionHash,
                JsonSerializer.Serialize(newRecord),
                expiry: CacheTime);
            var success = await transaction.ExecuteAsync();

            if (!success)
            {
                _logger.LogWarning("Transaction {transactionHash} status was changed in the cache by another process while trying to set it to {newStatus} old known state {cacheState}, change aborted.", transactionHash, newRecord.NewStatus, cacheRecord.NewStatus);
            }
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
