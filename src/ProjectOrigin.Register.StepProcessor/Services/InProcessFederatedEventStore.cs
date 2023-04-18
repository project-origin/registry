
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Register.StepProcessor.Services;

public class InProcessFederatedEventStore : IFederatedEventStore
{
    private IEventStore _localBatcher;
    private Dictionary<string, IEventStore> _eventStores;

    public InProcessFederatedEventStore(IEventStore localBatcher, Dictionary<string, IEventStore> eventStores)
    {
        _localBatcher = localBatcher;
        _eventStores = eventStores;
    }

    public Task PublishEvent(VerifiableEvent e)
    {
        return _localBatcher.Store(e);
    }

    public async Task<IDictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>>> GetStreams(IEnumerable<V1.FederatedStreamId> streamsIds)
    {
        var result = (await Task.WhenAll(
                    streamsIds.Select(async federatedId =>
                    {
                        var eventStore = _eventStores.GetValueOrDefault(federatedId.Registry);
                        if (eventStore is null)
                            throw new NullReferenceException($"Connection to EventStore for registry ”{federatedId.Registry}” could not be found");

                        var streamId = Guid.Parse(federatedId.StreamId.Value);
                        var verifiableEvents = await eventStore.GetEventsForEventStream(streamId);
                        var signedEvents = verifiableEvents.Select(verifiableEvent => V1.SignedEvent.Parser.ParseFrom(verifiableEvent.Content));

                        return (federatedId, signedEvents);
                    }))
            )
            .ToDictionary(
                tuple => tuple.federatedId,
                tuple => tuple.signedEvents);

        return result;
    }
}
