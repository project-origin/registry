using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.RequestProcessor.Interfaces;

public interface IEventSerializer
{
    object Deserialize(Event e);

    Event Serialize(EventId id, object e);

    byte[] Serialize(object e);
}
