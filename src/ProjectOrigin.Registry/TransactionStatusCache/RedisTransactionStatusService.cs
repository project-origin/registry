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
        var cacheStatus = await GetRecordAsync(transactionHash, CommandFlags.PreferMaster);
        if (cacheStatus is not null)
        {
            return cacheStatus.Record;
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

        var cacheStatus = await GetRecordAsync(transactionHash, CommandFlags.DemandMaster);

        if (newRecord.NewStatus < cacheStatus?.Record.NewStatus)
        {
            _logger.LogWarning("Transaction {transactionHash} status in cache is {oldStatus} and is higher than {newStatus}, change aborted.", transactionHash, cacheStatus.Record.NewStatus, newRecord.NewStatus);
            return;
        }

        await SafeSetRecord(transactionHash, newRecord, cacheStatus);
    }

    private async Task SafeSetRecord(TransactionHash transactionHash, TransactionStatusRecord newRecord, CacheRecord? cacheRecord)
    {

        var serializedRecord = Serialize(newRecord);
        if (cacheRecord is null)
        {
            if (await TrySetNewRecordAsync(transactionHash, serializedRecord))
                return;

            _logger.LogError("Transaction {transactionHash} status was set by another process while setting to {newStatus}, change aborted.", transactionHash, newRecord.NewStatus);
        }
        else
        {
            if (await TryUpdateExistingRecordAsync(transactionHash, serializedRecord, cacheRecord.SerializedRecord))
                return;

            _logger.LogError("Transaction {transactionHash} status was changed by another process while setting to {newStatus}, old state {cacheState}, change aborted.", transactionHash, serializedRecord, cacheRecord.SerializedRecord);
        }
    }

    private async Task<bool> TrySetNewRecordAsync(TransactionHash transactionHash, string newRecord)
    {
        var redisDatabase = _connectionMultiplexer.GetDatabase();

        return await redisDatabase.StringSetAsync(
            transactionHash,
            newRecord,
            when: When.NotExists,
            flags: CommandFlags.DemandMaster,
            expiry: CacheTime);
    }

    private async Task<bool> TryUpdateExistingRecordAsync(TransactionHash transactionHash, string newRecord, string cacheRecord)
    {
        var redisDatabase = _connectionMultiplexer.GetDatabase();

        var transaction = redisDatabase.CreateTransaction();
        transaction.AddCondition(Condition.StringEqual(transactionHash, cacheRecord));
        _ = transaction.StringSetAsync(
            transactionHash,
            newRecord,
            flags: CommandFlags.DemandMaster,
            expiry: CacheTime);
        var success = await transaction.ExecuteAsync();

        if (!success)
        {
            var found = await GetRecordAsync(transactionHash, CommandFlags.PreferMaster);
            _logger.LogError("Failed to update transaction {transactionHash} status from {cacheRecord} to {newRecord}. Current cache state: {currentState}.", transactionHash, cacheRecord, newRecord, found?.SerializedRecord);
        }

        return success;
    }

    private async Task<CacheRecord?> GetRecordAsync(TransactionHash transactionHash, CommandFlags flags)
    {
        var redisDatabase = _connectionMultiplexer.GetDatabase();

        var redisValue = await redisDatabase.StringGetAsync(transactionHash, flags);

        if (redisValue.HasValue)
        {
            await redisDatabase.KeyExpireAsync(transactionHash, CacheTime);
            var deserialized = JsonSerializer.Deserialize<TransactionStatusRecord>(redisValue!);
            if (deserialized is not null)
            {
                return new CacheRecord(deserialized, redisValue!);
            }
            else
            {
                _logger.LogError("Failed to deserialize TransactionStatusRecord from Redis for transaction {transactionHash}.", transactionHash);
                return null;
            }
        }
        else
            return null;
    }

    private static JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    private static string Serialize(TransactionStatusRecord record) => JsonSerializer.Serialize(record, JsonSerializerOptions);

    private record CacheRecord(TransactionStatusRecord Record, string SerializedRecord);
}
