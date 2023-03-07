using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Services.Batcher.Postgres;

public sealed class PostgresBatcher : IBatcher
{
    private readonly IEventStore _eventStore;

    public PostgresBatcher(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public Task PublishEvent(VerifiableEvent e)
    {
        return _eventStore.Store(e);

    }
}
