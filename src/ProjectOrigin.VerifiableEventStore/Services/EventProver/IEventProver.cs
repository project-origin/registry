using System.Threading.Tasks;

namespace ProjectOrigin.VerifiableEventStore.Services.EventProver;

public interface IEventProver
{
    Task<MerkleProof?> GetMerkleProof(string transactionId);
}
