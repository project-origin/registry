using System.Linq;
using System.Threading.Tasks;
using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Services.EventProver;

public class EventProverService : IEventProver
{
    private IEventStore _eventStore;

    public EventProverService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<MerkleProof?> GetMerkleProof(string transactionId)
    {
        var batch = await _eventStore.GetBatchFromTransactionId(transactionId);
        if (batch is null)
        {
            return null;
        }

        var events = await _eventStore.GetEventsForBatch(batch.Id);

        var eventObj = events.Single(e => e.TransactionId == transactionId);
        var leafIndex = events.TakeWhile(x => !x.Equals(eventObj)).Count();
        var hashes = events.GetRequiredHashes(e => e.Content, leafIndex);

        return new MerkleProof(transactionId, eventObj.Content, batch.BlockId, batch.TransactionId, leafIndex, hashes);
    }
}
