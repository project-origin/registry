using Google.Protobuf;
using Microsoft.Extensions.Options;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;

namespace ProjectOrigin.Register.StepProcessor.Services;

public class CommandStepProcessor : ICommandStepProcessor
{
    private CommandStepProcessorOptions _options;
    private ICommandStepVerifier _verifier;
    private IBatcher _batcher;

    public CommandStepProcessor(IOptions<CommandStepProcessorOptions> options, ICommandStepVerifier verifier, IBatcher batcher)
    {
        _options = options.Value;
        _verifier = verifier;
        _batcher = batcher;
    }

    public async Task<V1.CommandStepStatus> Process(V1.CommandStep request)
    {
        if (request.RoutingId.Registry != _options.RegistryName) throw new InvalidDataException("Invalid registry for request");

        var streams = await GetStreams(request.OtherStreams.Append(request.RoutingId));
        var result = await _verifier.Verify(request, streams);

        switch (result)
        {
            case VerificationResult.Valid valid:
                await PublishEvent(request, streams);

                return new V1.CommandStepStatus()
                {
                    State = V1.CommandState.Succeeded,
                    Error = string.Empty,
                };

            case VerificationResult.Invalid invalid:
                return new V1.CommandStepStatus()
                {
                    State = V1.CommandState.Failed,
                    Error = invalid.ErrorMessage,
                };

            default:
                throw new NotImplementedException($"Unsupporetd VerificationResult type ”{result.GetType().Name}”");
        }
    }

    private async Task PublishEvent(V1.CommandStep request, Dictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>> streams)
    {
        var streamId = Guid.Parse(request.RoutingId.StreamId.Value);
        var nextEventIndex = streams[request.RoutingId].Count();
        var eventId = new EventId(streamId, nextEventIndex);

        var verifiableEvent = new VerifiableEvent(eventId, request.SignedEvent.ToByteArray());
        await _batcher.PublishEvent(verifiableEvent);
    }

    public async Task<Dictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>>> GetStreams(IEnumerable<V1.FederatedStreamId> streamsIds)
    {
        return (await Task.WhenAll(
                    streamsIds.Select(async federatedId =>
                    {
                        var eventStore = _options.EventStores.GetValueOrDefault(federatedId.Registry);
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
    }
}
