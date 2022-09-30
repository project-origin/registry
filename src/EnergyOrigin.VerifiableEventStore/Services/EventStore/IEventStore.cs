namespace EnergyOrigin.VerifiableEventStore.Services.EventStore;

public interface IEventStore
{
    Task StoreBatch(Batch batch);
    Task<Batch?> GetBatch(Guid eventId);
}
