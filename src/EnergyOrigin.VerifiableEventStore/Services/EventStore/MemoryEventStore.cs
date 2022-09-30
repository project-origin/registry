namespace EnergyOrigin.VerifiableEventStore.Services.EventStore;

public class MemoryEventStore : IEventStore
{
    private List<Batch> batches = new List<Batch>();

    public Task StoreBatch(Batch batch)
    {
        batches.Add(batch);
        return Task.CompletedTask;
    }

    public Task<Batch?> GetBatch(Guid eventId)
    {
        var batch = batches.Where(b => b.Events.Select(e => e.Id).Contains(eventId)).SingleOrDefault();
        return Task.FromResult(batch);
    }
}
