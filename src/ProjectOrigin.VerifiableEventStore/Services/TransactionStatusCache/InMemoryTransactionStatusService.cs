using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Repository;

namespace ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

public class InMemoryTransactionStatusService : ITransactionStatusService
{
    private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan UnknownCacheTime = TimeSpan.FromMinutes(5);

    private readonly ILogger<InMemoryTransactionStatusService> _logger;
    private readonly IDistributedCache _cache;
    private readonly ITransactionRepository _transactionRepository;
    private readonly object _lock = new();

    public InMemoryTransactionStatusService(ILogger<InMemoryTransactionStatusService> logger, IDistributedCache cache, ITransactionRepository eventStore)
    {
        _logger = logger;
        _cache = cache;
        _transactionRepository = eventStore;
    }

    public async Task<TransactionStatusRecord> GetTransactionStatus(TransactionHash transactionHash)
    {
        var cacheStatus = GetRecord(transactionHash);
        if (cacheStatus is not null)
        {
            return cacheStatus;
        }
        else
        {
            var dbStatus = await _transactionRepository.GetTransactionStatus(transactionHash);
            var statusRecord = new TransactionStatusRecord(dbStatus);
            SafeSetRecord(transactionHash, statusRecord, null);

            return statusRecord;
        }
    }

    public Task SetTransactionStatus(TransactionHash transactionHash, TransactionStatusRecord newRecord)
    {
        _logger.LogTrace($"Setting transaction status for {transactionHash} to {newRecord.NewStatus}");

        var cacheStatus = GetRecord(transactionHash);

        if (newRecord.NewStatus < cacheStatus?.NewStatus)
        {
            _logger.LogWarning($"Transaction {transactionHash} status in cache is {cacheStatus.NewStatus} and is higher than {newRecord.NewStatus}, change aborted.");
            return Task.CompletedTask;
        }

        SafeSetRecord(transactionHash, newRecord, cacheStatus);
        return Task.CompletedTask;
    }

    private void SafeSetRecord(TransactionHash transactionHash, TransactionStatusRecord newRecord, TransactionStatusRecord? cacheRecord)
    {
        lock (_lock)
        {
            var foundRecord = GetRecord(transactionHash);
            if (foundRecord != cacheRecord)
            {
                _logger.LogWarning($"Transaction {transactionHash} status was changed in the cache by another process while trying to set it to {newRecord.NewStatus}, change aborted.");
            }
            else
            {
                _cache.SetStringAsync(transactionHash.ToString(), JsonSerializer.Serialize(newRecord), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = newRecord.NewStatus == TransactionStatus.Unknown ? UnknownCacheTime : CacheTime
                });
            }
        }
    }

    private TransactionStatusRecord? GetRecord(TransactionHash transactionHash)
    {
        var bytes = _cache.GetString(transactionHash.ToString());

        if (bytes is not null)
        {
            return JsonSerializer.Deserialize<TransactionStatusRecord>(bytes)
                ?? throw new InvalidOperationException("The deserialized transaction status record was null");
        }
        else
            return null;
    }
}

