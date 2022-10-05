using EnergyOrigin.VerifiableEventStore.Models;

namespace EnergyOrigin.VerifiableEventStore.Services.Batcher;

public interface IBatcher
{
    Task PublishEvent(Event e);
}
