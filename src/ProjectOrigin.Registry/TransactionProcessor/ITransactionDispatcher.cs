using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectOrigin.Registry.TransactionProcessor;

public interface ITransactionDispatcher
{
    Task<Verifier.V1.VerifyTransactionResponse> VerifyTransaction(V1.Transaction transaction, IEnumerable<V1.Transaction> stream);
}
