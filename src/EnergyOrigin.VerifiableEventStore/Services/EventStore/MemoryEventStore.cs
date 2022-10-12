using EnergyOrigin.VerifiableEventStore.Models;

namespace EnergyOrigin.VerifiableEventStore.Services.EventStore;

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

    public Task<IEnumerable<Event>> GetEventsForEventStream(Guid streamId)
    {
        var events = batches.SelectMany(b => b.Events.Where(e => e.Id.EventStreamId == streamId));
        return Task.FromResult(events);
    }
}