using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using ProjectOrigin.RequestProcessor.Interfaces;

namespace ProjectOrigin.RequestProcessor.Services;

public class ModelLoader : IModelLoader
{
    private ConcurrentDictionary<Type, Projector> projectorDictionary = new();
    private ModelLoaderOptions options;
    private IEventSerializer eventSerializer;

    public ModelLoader(IOptions<ModelLoaderOptions> options, IEventSerializer eventSerializer)
    {
        this.options = options.Value;
        this.eventSerializer = eventSerializer;
    }

    public async Task<(IModel? model, int eventCount)> Get(FederatedStreamId eventStreamId, Type type)
    {
        var projector = GetProjector(type);

        var eventStore = options.EventStores.GetValueOrDefault(eventStreamId.Registry);
        if (eventStore is null)
            throw new NullReferenceException($"Connection to EventStore for registry ”{eventStreamId.Registry}” could not be found");

        var events = await eventStore.GetEventsForEventStream(eventStreamId.StreamId);

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

    public async Task<(T? model, int eventCount)> Get<T>(FederatedStreamId eventStreamId) where T : class, IModel
    {
        var (model, eventCount) = await Get(eventStreamId, typeof(T));
        return (model as T, eventCount);
    }

    private IProjector GetProjector(Type type)
    {
        return projectorDictionary.GetOrAdd(type, (type) => new Projector(type));
    }
}
