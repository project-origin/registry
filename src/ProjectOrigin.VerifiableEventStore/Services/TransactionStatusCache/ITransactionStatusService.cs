using System.Threading.Tasks;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

public interface ITransactionStatusService
{
    Task SetTransactionStatus(TransactionHash transactionHash, TransactionStatusRecord record);
    Task<TransactionStatusRecord> GetTransactionStatus(TransactionHash transactionHash);
}
