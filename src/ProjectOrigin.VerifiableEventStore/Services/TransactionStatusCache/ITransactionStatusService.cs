using System.Threading.Tasks;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

public interface ITransactionStatusService
{
    Task SetTransactionStatus(string transactionId, TransactionStatusRecord record);
    Task<TransactionStatusRecord> GetTransactionStatus(string transactionId);
}
