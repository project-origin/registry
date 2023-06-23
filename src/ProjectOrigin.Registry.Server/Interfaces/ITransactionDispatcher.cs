using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectOrigin.Registry.Server.Interfaces;

public interface ITransactionDispatcher
{
    Task<Verifier.V1.VerifyTransactionResponse> VerifyTransaction(V1.Transaction request, IEnumerable<V1.Transaction> stream);
}
