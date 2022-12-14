using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore;

public class MemoryEventStore : IEventStore
{
    private List<Batch> _batches = new List<Batch>();

    public Task StoreBatch(Batch batch)
    {
        _batches.Add(batch);
        return Task.CompletedTask;
    }

    public Task<Batch?> GetBatch(EventId eventId)
    {
        var batch = _batches.Where(b => b.Events.Select(e => e.Id).Contains(eventId)).SingleOrDefault();
        return Task.FromResult(batch);
    }

    public Task<IEnumerable<VerifiableEvent>> GetEventsForEventStream(Guid streamId)
    {
        var events = _batches.SelectMany(b => b.Events.Where(e => e.Id.EventStreamId == streamId));
        return Task.FromResult(events);
    }
}
