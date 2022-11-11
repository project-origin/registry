using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore;

public class MemoryEventStore : IEventStore
{
    private List<Batch> batches = new List<Batch>();

    public Task StoreBatch(Batch batch)
    {
        batches.Add(batch);
        return Task.CompletedTask;
    }

    public Task<Batch?> GetBatch(EventId eventId)
    {
        var batch = batches.Where(b => b.Events.Select(e => e.Id).Contains(eventId)).SingleOrDefault();
        return Task.FromResult(batch);
    }

    public Task<IEnumerable<VerifiableEvent>> GetEventsForEventStream(Guid streamId)
    {
        var events = batches.SelectMany(b => b.Events.Where(e => Guid.Parse(e.Id.EventStreamId.Value) == streamId));
        return Task.FromResult(events);
    }
}
