using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

public class TransactionStatusService : ITransactionStatusService
{
    public IDistributedCache _cache;
    private IEventStore _eventStore;

    public TransactionStatusService(IDistributedCache cache, IEventStore eventStore)
    {
        _cache = cache;
        _eventStore = eventStore;
    }

    public async Task<TransactionStatusRecord> GetTransactionStatus(string transactionId)
    {
        var bytes = await _cache.GetStringAsync(transactionId);
        if (bytes is not null)
        {
            return JsonSerializer.Deserialize<TransactionStatusRecord>(bytes) ?? throw new System.Exception();
        }
        else
        {
            var a = _eventStore.GetTransactionStatus(transactionId);
            return new TransactionStatusRecord(TransactionStatus.Unknown);
        }
    }

    public Task SetTransactionStatus(string transactionId, TransactionStatusRecord record)
    {
        return _cache.SetStringAsync(transactionId, JsonSerializer.Serialize(record));
    }
}
