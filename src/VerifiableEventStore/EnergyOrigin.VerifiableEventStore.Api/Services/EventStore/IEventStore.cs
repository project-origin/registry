namespace EnergyOrigin.VerifiableEventStore.Api.Services.EventStore;

public interface IEventStore
{
    Task<int> StoreBatch(Batch batch);
    Task<Batch> GetBatch(Guid eventId);
}
