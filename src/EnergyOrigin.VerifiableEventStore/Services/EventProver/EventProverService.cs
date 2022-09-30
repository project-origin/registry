using EnergyOrigin.VerifiableEventStore.Extensions;
using EnergyOrigin.VerifiableEventStore.Services.EventStore;

namespace EnergyOrigin.VerifiableEventStore.Services.EventProver;

public class EventProverService : IEventProver
{
    private IEventStore eventStore;

    public EventProverService(IEventStore eventStore)
    {
        this.eventStore = eventStore;
    }

    public async Task<MerkleProof?> GetMerkleProof(Guid eventId)
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
