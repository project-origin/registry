using System.Threading.Tasks;

namespace ProjectOrigin.Registry.Server;

public interface ITransactionStatusService
{
    Task SetTransactionStatus(V1.TransactionId transactionId, V1.Internal.TransactionStatus state);
    Task<V1.Internal.TransactionStatus> GetTransactionStatus(V1.TransactionId transactionId);
}
