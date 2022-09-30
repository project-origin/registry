namespace EnergyOrigin.VerifiableEventStore.Services.Batcher;

public interface IBatcher
{
    Task PublishEvent(PublishEventRequest request);
}
