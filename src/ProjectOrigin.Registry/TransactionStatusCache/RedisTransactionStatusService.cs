using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.Repository;
using ProjectOrigin.Registry.Repository.Models;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;

namespace ProjectOrigin.Registry.TransactionStatusCache;

public class RedisTransactionStatusService : ITransactionStatusService
{
    private readonly ILogger<RedisTransactionStatusService> _logger;
    private readonly IFusionCache _cache;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisTransactionStatusService(
        ILogger<RedisTransactionStatusService> logger,
        IFusionCache cache,
        ITransactionRepository transactionRepository,
        IConnectionMultiplexer connectionMultiplexer)
    {
        _logger = logger;
        _cache = cache;
        _transactionRepository = transactionRepository;
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<TransactionStatusRecord> GetTransactionStatus(TransactionHash transactionHash)
    {
        var key = transactionHash.ToString();

        return await _cache.GetOrSetAsync<TransactionStatusRecord>(
            key,
            async _ =>
            {
                var dbStatus = await _transactionRepository.GetTransactionStatus(transactionHash);
                return new TransactionStatusRecord(dbStatus);
            });
    }

    public async Task SetTransactionStatus(TransactionHash transactionHash, TransactionStatusRecord newRecord)
    {
        var key = transactionHash.ToString();
        _logger.LogTrace("Setting status for {transactionHash} to {newStatus}", transactionHash, newRecord.NewStatus);

        var redis = _connectionMultiplexer.GetDatabase();

        var redisValue = await redis.StringGetAsync(key);
        TransactionStatusRecord? current = null;

        if (redisValue.HasValue)
            current = JsonSerializer.Deserialize<TransactionStatusRecord>(redisValue!);

        if (current != null && newRecord.NewStatus < current.NewStatus)
        {
            _logger.LogWarning("Status downgrade prevented for {transactionHash}", transactionHash);
            return;
        }

        var serializedNew = JsonSerializer.Serialize(newRecord);
        var transaction = redis.CreateTransaction();

        if (current == null)
        {
            transaction.AddCondition(Condition.KeyNotExists(key));
        }
        else
        {
            var serializedCurrent = JsonSerializer.Serialize(current);
            transaction.AddCondition(Condition.StringEqual(key, serializedCurrent));
        }

        _ = transaction.StringSetAsync(key, serializedNew, flags: CommandFlags.DemandMaster);
        bool isCommitted = await transaction.ExecuteAsync();

        if (isCommitted)
        {
            await _cache.RemoveAsync(key);
            await _cache.SetAsync(key, newRecord);
        }
        else
        {
            var latestValue = await redis.StringGetAsync(key);
            if (!latestValue.HasValue)
            {
                _logger.LogError("Concurrent modification detected. Initial state was null. Update aborted for {transactionHash}", transactionHash);
                return;
            }

            var latest = JsonSerializer.Deserialize<TransactionStatusRecord>(latestValue!);

            if (current == null && latest!.NewStatus == TransactionStatus.Unknown)
            {
                _logger.LogWarning("Concurrent modification detected. Existing state was unknown. Update aborted for {transactionHash}", transactionHash);
            }
            else
            {
                _logger.LogError("Concurrent modification detected. Existing state was {status}. Update aborted for {transactionHash}", latest!.NewStatus, transactionHash);
            }
        }
    }
}
