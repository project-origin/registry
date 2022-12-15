using Google.Protobuf;
using Microsoft.Extensions.Options;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.Register.StepProcessor.Services;

public class CommandStepProcessor : ICommandStepProcessor
{
    private CommandStepProcessorOptions _options;
    private IFederatedEventStore _federatedEventStore;
    private ICommandStepVerifiere _verifier;

    public CommandStepProcessor(IOptions<CommandStepProcessorOptions> options, IFederatedEventStore federatedEventStore, ICommandStepVerifiere verifier)
    {
        _options = options.Value;
        _federatedEventStore = federatedEventStore;
        _verifier = verifier;
    }

    public async Task<V1.CommandStepStatus> Process(V1.CommandStep request)
    {
        if (request.RoutingId.Registry != _options.RegistryName) throw new InvalidDataException("Invalid registry for request");

        var streams = await _federatedEventStore.GetStreams(request.OtherStreams.Append(request.RoutingId));
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

    private async Task PublishEvent(V1.CommandStep request, IDictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>> streams)
    {
        var streamId = Guid.Parse(request.RoutingId.StreamId.Value);
        var nextEventIndex = streams[request.RoutingId].Count();
        var eventId = new EventId(streamId, nextEventIndex);

        var verifiableEvent = new VerifiableEvent(eventId, request.SignedEvent.ToByteArray());
        await _federatedEventStore.PublishEvent(verifiableEvent);
    }
}
