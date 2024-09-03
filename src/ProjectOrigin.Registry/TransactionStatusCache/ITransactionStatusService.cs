using System.Threading.Tasks;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

public interface ITransactionStatusService
{
    Task SetTransactionStatus(TransactionHash transactionHash, TransactionStatusRecord newRecord);
    Task<TransactionStatusRecord> GetTransactionStatus(TransactionHash transactionHash);
}
