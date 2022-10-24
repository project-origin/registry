using System.Collections.Concurrent;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.RequestProcessor.Services;

public class ModelLoader : IModelLoader
{
    private ConcurrentDictionary<Type, Projector> projectorDictionary;
    private IEventStore eventStore;
    private IEventSerializer eventSerializer;

    public ModelLoader(IEventStore eventStore, IEventSerializer eventSerializer)
    {
        projectorDictionary = new();
        this.eventStore = eventStore;
        this.eventSerializer = eventSerializer;
    }

    public async Task<(IModel? model, int eventCount)> Get(Guid eventStreamId, Type type)
    {
        var projector = GetProjector(type);

        var events = await eventStore.GetEventsForEventStream(eventStreamId);

        if (events.Any())
        {
            var deserializedEvents = events.Select(e => eventSerializer.Deserialize(e));
            return (projector.Project(deserializedEvents), deserializedEvents.Count());
        }
        else
        {
            return (null, 0);
        }
    }

    private IProjector GetProjector(Type type)
    {
        return projectorDictionary.GetOrAdd(type, (type) => new Projector(type));
    }
}
