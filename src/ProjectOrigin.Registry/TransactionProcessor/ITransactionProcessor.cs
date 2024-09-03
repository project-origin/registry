using System.Threading.Tasks;

namespace ProjectOrigin.Registry.Server.Interfaces;

public interface ITransactionProcessor
{
    Task ProcessTransaction(V1.Transaction transaction);
}
