using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore;

public interface IEventStore
{
    Task StoreBatch(Batch batch);
    Task Store(VerifiableEvent @event);
    Task<Batch?> GetBatch(EventId eventId);
    Task<IEnumerable<VerifiableEvent>> GetEventsForEventStream(Guid topic);
}
