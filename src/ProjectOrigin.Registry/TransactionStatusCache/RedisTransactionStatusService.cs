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
        if (cacheRecord is null)
        {
            if (await TrySetNewRecordAsync(transactionHash, newRecord))
                return;

            var cacheStatus = await GetRecordAsync(transactionHash);
            if (cacheStatus == null)
            {
                _logger.LogError("Transaction {transactionHash} status was null, still null, but failed setting to {newStatus}, retrying.", transactionHash, newRecord.NewStatus);

            }
            else if (cacheStatus.NewStatus == TransactionStatus.Unknown)
            {
                _logger.LogWarning("Transaction {transactionHash} status was unknown while setting to {newStatus}, retrying.", transactionHash, newRecord.NewStatus);
                await SafeSetRecord(transactionHash, newRecord, cacheRecord);
            }
            else
            {
                _logger.LogError("Transaction {transactionHash} status was set by another process while setting to {newStatus}, change aborted.", transactionHash, newRecord.NewStatus);
            }
        }
        else
        {
            if (await TryUpdateExistingRecordAsync(transactionHash, newRecord, cacheRecord))
                return;

            var foundRecord = await GetRecordAsync(transactionHash) ?? throw new InvalidOperationException($"Transaction {transactionHash} status was not found in cache after update attempt.");
            _logger.LogWarning("Transaction {transactionHash} status was changed by another process while setting to {newStatus}, old state {cacheState} found state {foundState}, change aborted.", transactionHash, newRecord.NewStatus, cacheRecord.NewStatus, foundRecord.NewStatus);

            if (await TryUpdateExistingRecordAsync(transactionHash, newRecord, foundRecord))
                return;

            _logger.LogError("Transaction {transactionHash} status was changed by another process while setting to {newStatus}, old state {cacheState}, change aborted.", transactionHash, newRecord.NewStatus, foundRecord.NewStatus);
        }
    }

    private async Task<bool> TrySetNewRecordAsync(TransactionHash transactionHash, TransactionStatusRecord newRecord)
    {
        var redisDatabase = _connectionMultiplexer.GetDatabase();

        return await redisDatabase.StringSetAsync(
            transactionHash,
            JsonSerializer.Serialize(newRecord),
            when: When.NotExists,
            expiry: CacheTime);
    }

    private async Task<bool> TryUpdateExistingRecordAsync(TransactionHash transactionHash, TransactionStatusRecord newRecord, TransactionStatusRecord cacheRecord)
    {
        var redisDatabase = _connectionMultiplexer.GetDatabase();

        var transaction = redisDatabase.CreateTransaction();
        transaction.AddCondition(Condition.StringEqual(transactionHash, JsonSerializer.Serialize(cacheRecord)));
        _ = transaction.StringSetAsync(
            transactionHash,
            JsonSerializer.Serialize(newRecord),
            expiry: CacheTime);
        return await transaction.ExecuteAsync();
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
