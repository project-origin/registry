using System.Linq;
using System.Threading.Tasks;
using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Repository;

namespace ProjectOrigin.VerifiableEventStore.Services.EventProver;

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
