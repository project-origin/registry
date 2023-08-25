using System;
using System.Linq;
using System.Threading.Tasks;
using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Services.EventProver;

public class EventProverService : IEventProver
{
    private IEventStore _eventStore;

    public EventProverService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<MerkleProof?> GetMerkleProof(TransactionHash transactionHash)
    {
        var batch = await _eventStore.GetBatchFromTransactionHash(transactionHash);
        if (batch is null)
        {
            return null;
        }

        var events = await _eventStore.GetEventsForBatch(BatchHash.FromHeader(batch.Header));

        var eventObj = events.Single(e => e.TransactionHash == transactionHash);
        var leafIndex = events.IndexOf(eventObj);
        var hashes = events.GetRequiredHashes(e => e.Payload, leafIndex);

        return new MerkleProof(transactionHash, eventObj.Payload, leafIndex, hashes);
    }
}
