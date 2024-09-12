using System.Threading.Tasks;
using ProjectOrigin.Registry.Repository.Models;

namespace ProjectOrigin.Registry.TransactionStatusCache;

public interface ITransactionStatusService
{
    Task SetTransactionStatus(TransactionHash transactionHash, TransactionStatusRecord newRecord);
    Task<TransactionStatusRecord> GetTransactionStatus(TransactionHash transactionHash);
}
