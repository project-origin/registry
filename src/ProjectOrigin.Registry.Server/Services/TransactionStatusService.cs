using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Distributed;

namespace ProjectOrigin.Registry.Server;

public class TransactionStatusService : ITransactionStatusService
{
    public IDistributedCache _cache;

    public TransactionStatusService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<V1.Internal.TransactionStatus> GetTransactionStatus(V1.TransactionId transactionId)
    {
        var bytes = await _cache.GetAsync(transactionId.Value);
        if (bytes is not null)
        {
            return V1.Internal.TransactionStatus.Parser.ParseFrom(bytes);
        }
        else
        {
            return new V1.Internal.TransactionStatus()
            {
                State = V1.TransactionState.Unknown
            };
        }
    }

    public Task SetTransactionStatus(V1.TransactionId transactionId, V1.Internal.TransactionStatus status)
    {
        return _cache.SetAsync(transactionId.Value, status.ToByteArray());
    }
}
