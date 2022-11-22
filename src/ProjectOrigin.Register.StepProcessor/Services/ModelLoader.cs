using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Register.StepProcessor.Services;

public class ModelLoader : IModelLoader
{
    private ConcurrentDictionary<Type, ModelProjector> _projectorDictionary = new();

    private ModelLoaderOptions _options;

    public ModelLoader(IOptions<ModelLoaderOptions> options)
    {
        _options = options.Value;
    }

    public async Task<(IModel? model, int eventCount)> Get(FederatedStreamId eventStreamId, Type type)
    {
        var projector = GetProjector(type);

        var eventStore = _options.EventStores.GetValueOrDefault(eventStreamId.Registry);
        if (eventStore is null)
            throw new NullReferenceException($"Connection to EventStore for registry ”{eventStreamId.Registry}” could not be found");

        var events = await eventStore.GetEventsForEventStream(eventStreamId.StreamId);

        if (events.Any())
        {
            var deserializedEvents = events.Select(e => SignedEvent.Deserialize(e.Content).Event).ToList();
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

    private IModelProjector GetProjector(Type type)
    {
        return _projectorDictionary.GetOrAdd(type, (type) => new ModelProjector(type));
    }
}
