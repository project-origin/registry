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
    private const int MaxRetries = 5;
    private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(60);

    private static readonly LuaScript AtomicUpdateScript = LuaScript.Prepare(
        @"local current = redis.call('GET', KEYS[1])
          local newStatus = tonumber(ARGV[1])

          if current == false then
              redis.call('SET', KEYS[1], ARGV[2], 'EX', ARGV[3])
              return {1, 'CREATED'}
          end

          local currentRecord
          pcall(function()
              currentRecord = cjson.decode(current)
          end)
          if not currentRecord then
              return {0, 'INVALID_CURRENT_STATE'}
          end

          if newStatus < currentRecord.NewStatus then
              return {0, 'STATUS_DOWNGRADE'}
          end

          redis.call('SET', KEYS[1], ARGV[2], 'EX', ARGV[3])
          return {1, 'UPDATED'}"
    );

    private readonly ILogger<RedisTransactionStatusService> _logger;
    private readonly IDatabase _db;
    private readonly ITransactionRepository _transactionRepository;

    public RedisTransactionStatusService(
        ILogger<RedisTransactionStatusService> logger,
        IConnectionMultiplexer connectionMultiplexer,
        ITransactionRepository transactionRepository)
    {
        _logger = logger;
        _db = connectionMultiplexer.GetDatabase();
        _transactionRepository = transactionRepository;
    }

    public async Task<TransactionStatusRecord> GetTransactionStatus(TransactionHash transactionHash)
    {
        var record = await GetRecordWithConsistencyAsync(transactionHash);
        if (record != null) return record;

        var dbStatus = await _transactionRepository.GetTransactionStatus(transactionHash);
        var newRecord = new TransactionStatusRecord(dbStatus);

        var result = await TrySetRecordAtomic(transactionHash, newRecord);
        if (result.Success) return newRecord;

        return (await GetRecordWithConsistencyAsync(transactionHash)) ?? newRecord;
    }

    public async Task SetTransactionStatus(TransactionHash transactionHash, TransactionStatusRecord newRecord)
    {
        _logger.LogTrace("Updating status for {hash} to {status}",
            transactionHash, newRecord.NewStatus);

        int attempt = 0;
        while (attempt < MaxRetries)
        {
            var result = await TrySetRecordAtomic(transactionHash, newRecord);

            if (result.Success) return;

            if (result.Error == "STATUS_DOWNGRADE")
            {
                _logger.LogWarning("Status downgrade rejected for {hash}", transactionHash);
                return;
            }

            attempt++;
            await Task.Delay(CalculateBackoff(attempt));
        }

        _logger.LogError("Failed to update status for {hash} after {attempts} attempts",
            transactionHash, MaxRetries);
    }

    private async Task<TransactionStatusRecord?> GetRecordWithConsistencyAsync(TransactionHash transactionHash)
    {
        var redisValue = await _db.StringGetAsync(
            transactionHash,
            flags: CommandFlags.DemandMaster
        );

        if (!redisValue.HasValue) return null;

        await _db.KeyExpireAsync(transactionHash, CacheTime, flags: CommandFlags.DemandMaster);
        return JsonSerializer.Deserialize<TransactionStatusRecord>(redisValue!);
    }

    private async Task<(bool Success, string Error)> TrySetRecordAtomic(
        TransactionHash transactionHash,
        TransactionStatusRecord newRecord)
    {
        var serializedNew = JsonSerializer.Serialize(newRecord);
        var expirySeconds = (int)CacheTime.TotalSeconds;

        var parameters = new
        {
            keys = new RedisKey[] { transactionHash },
            argv = new RedisValue[]
            {
                (int)newRecord.NewStatus,
                serializedNew,
                expirySeconds
            }
        };

        try
        {
            RedisResult rawResult = await AtomicUpdateScript.EvaluateAsync(
                db: _db,
                ps: parameters,
                flags: CommandFlags.DemandMaster);

            object boxed = rawResult;

            if (boxed is RedisResult[] { Length: >= 2 } resultArray)
            {
                bool success = (int)resultArray[0] == 1;
                string message = resultArray[1].ToString();
                return (success, success ? string.Empty : message);
            }

            return (false, "INVALID_RESPONSE");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Atomic update failed for {hash}", transactionHash);
            return (false, "EXECUTION_ERROR");
        }
    }

    private TimeSpan CalculateBackoff(int attempt) =>
        TimeSpan.FromMilliseconds(50 * Math.Pow(2, attempt));
}
