namespace EnergyOrigin.VerifiableEventStore.Api.Services.EventStore;

public interface IEventStore
{
    Task StoreBatch(Batch batch);
    Task<Batch?> GetBatch(Guid eventId);
}
