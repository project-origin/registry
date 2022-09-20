namespace EnergyOrigin.VerifiableEventStore.Api.Services.Batcher;

public interface IBatcher
{
    Task PublishEvent(PublishEventRequest request);
}
