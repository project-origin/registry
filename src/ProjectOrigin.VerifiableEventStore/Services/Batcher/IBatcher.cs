using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.Batcher;

public interface IBatcher
{
    Task PublishEvent(Event e);
}
