using System.Threading.Tasks;
using ProjectOrigin.Registry.Repository.Models;

namespace ProjectOrigin.Registry.Grpc.EventProver;

public interface IEventProver
{
    Task<MerkleProof?> GetMerkleProof(TransactionHash transactionHash);
}
