using System.Threading.Tasks;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventProver;

public interface IEventProver
{
    Task<MerkleProof?> GetMerkleProof(TransactionHash transactionHash);
}
