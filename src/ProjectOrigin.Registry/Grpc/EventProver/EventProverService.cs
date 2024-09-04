using System.Linq;
using System.Threading.Tasks;
using ProjectOrigin.Registry.Extensions;
using ProjectOrigin.Registry.Repository;
using ProjectOrigin.Registry.Repository.Models;

namespace ProjectOrigin.Registry.Grpc.EventProver;

public class EventProverService : IEventProver
{
    private readonly ITransactionRepository _transactionRepository;

    public EventProverService(ITransactionRepository eventStore)
    {
        _transactionRepository = eventStore;
    }

    public async Task<MerkleProof?> GetMerkleProof(TransactionHash transactionHash)
    {
        var block = await _transactionRepository.GetBlock(transactionHash);
        if (block is null)
        {
            return null;
        }

        var events = await _transactionRepository.GetStreamTransactionsForBlock(BlockHash.FromHeader(block.Header));

        var eventObj = events.Single(e => e.TransactionHash == transactionHash);
        var leafIndex = events.IndexOf(eventObj);
        var hashes = events.GetRequiredHashes(e => e.Payload, leafIndex);

        return new MerkleProof(transactionHash, eventObj.Payload, leafIndex, hashes);
    }
}
