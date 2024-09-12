using System.Threading.Tasks;

namespace ProjectOrigin.Registry.TransactionProcessor;

public interface ITransactionProcessor
{
    Task ProcessTransaction(V1.Transaction transaction);
}
