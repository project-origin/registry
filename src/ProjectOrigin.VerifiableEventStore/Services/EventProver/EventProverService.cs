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
        var hashes = batch.Events.GetRequiredHashes(e => Serializer.SerializeProto(e), eventIndex);

        return CreateMerkleProof(eventObj, batch.TransactionId, eventIndex, hashes);
    }

    private static MerkleProof CreateMerkleProof(VerifiableEvent eventObj, string TransactionId, int leafIndex, IEnumerable<byte[]> hashes)
    {
        var proof = new MerkleProof()
        {
            Event = eventObj,
            BlockchainReference = new()
            {
                TransactionHash = TransactionId
            },
            LeafIndex = leafIndex
        };

        proof.Hashes.AddRange(hashes.Select(x => Google.Protobuf.ByteString.CopyFrom(x)));

        return proof;
    }
}
