using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Repository;

namespace ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

public class TransactionStatusService : ITransactionStatusService
{
    private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan UnknownCacheTime = TimeSpan.FromMinutes(5);

    private ILogger<TransactionStatusService> _logger;
    private IDistributedCache _cache;
    private ITransactionRepository _transactionRepository;

    public TransactionStatusService(ILogger<TransactionStatusService> logger, IDistributedCache cache, ITransactionRepository eventStore)
    {
        _logger = logger;
        _cache = cache;
        _transactionRepository = eventStore;
    }

    public async Task<TransactionStatusRecord> GetTransactionStatus(TransactionHash transactionHash)
    {
        var transactionId = Convert.ToBase64String(transactionHash.Data);
        var bytes = await _cache.GetStringAsync(transactionId);
        if (bytes is not null)
        {
            return JsonSerializer.Deserialize<TransactionStatusRecord>(bytes)
                ?? throw new InvalidOperationException("The deserialized transaction status record was null");
        }
        else
        {
            var status = await _transactionRepository.GetTransactionStatus(transactionHash);
            var statusRecord = new TransactionStatusRecord(status);

            if (status != TransactionStatus.Unknown)
                await SetTransactionStatus(transactionHash, statusRecord);

            return statusRecord;
        }
    }

    public Task SetTransactionStatus(TransactionHash transactionHash, TransactionStatusRecord record)
    {
        var transactionId = Convert.ToBase64String(transactionHash.Data);
        _logger.LogTrace($"Setting transaction status for {transactionId} to {record.NewStatus}");
        return _cache.SetStringAsync(transactionId, JsonSerializer.Serialize(record), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = record.NewStatus == TransactionStatus.Unknown ? UnknownCacheTime : CacheTime
        });
    }
}
