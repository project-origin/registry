using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Services.EventProver;

public class EventProverService : IEventProver
{
    private IEventStore eventStore;

    public EventProverService(IEventStore eventStore)
    {
        this.eventStore = eventStore;
    }

    public async Task<MerkleProof?> GetMerkleProof(EventId eventId)
    {
        var batch = await eventStore.GetBatch(eventId);
        if (batch is null)
        {
            return null;
        }

        var eventObj = batch.Events.Single(e => e.Id == eventId);
        var eventIndex = batch.Events.IndexOf(eventObj);
        var hashes = batch.Events.GetRequiredHashes(e => e.Content, eventIndex);

        return new MerkleProof(eventId, eventObj.Content, batch.BlockId, batch.TransactionId, batch.Events.IndexOf(eventObj), hashes);
    }
}
