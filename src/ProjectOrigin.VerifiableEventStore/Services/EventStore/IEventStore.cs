using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore;

public interface IEventStore
{
    Task StoreBatch(Batch batch);
    Task<Batch?> GetBatch(EventId eventId);
    Task<IEnumerable<Event>> GetEventsForEventStream(Guid topic);
}
