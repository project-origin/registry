using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore;

public interface IEventStore
{
    Task StoreBatch(Batch batch);
    Task Store(VerifiableEvent @event);
    Task<Batch?> GetBatch(EventId eventId);
    Task<IEnumerable<VerifiableEvent>> GetEventsForEventStream(Guid streamId);
    Task<IEnumerable<VerifiableEvent>> GetEventsForBatch(Guid batchId);
    public Task<IEnumerable<Guid>> GetBatchesForFinalization();
    Task FinalizeBatch(Guid batchId, string blockId, string transactionHash);
}
