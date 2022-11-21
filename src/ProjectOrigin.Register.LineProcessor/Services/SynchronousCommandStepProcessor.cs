using Microsoft.Extensions.Options;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;

namespace ProjectOrigin.Register.LineProcessor.Services;

public class SynchronousCommandStepProcessor : ICommandStepProcessor
{
    private CommandStepProcessorOptions options;
    private ICommandStepDispatcher dispatcher;
    private IBatcher batcher;

    public SynchronousCommandStepProcessor(IOptions<CommandStepProcessorOptions> options, ICommandStepDispatcher dispatcher, IBatcher batcher)
    {
        this.options = options.Value;
        this.dispatcher = dispatcher;
        this.batcher = batcher;
    }

    public async Task<CommandStepResult> Process(CommandStep request)
    {
        if (request.FederatedStreamId.Registry != options.RegistryName) throw new InvalidDataException("Invalid registry for request");

        var (result, nextEventIndex) = await dispatcher.Verify(request);

        switch (result)
        {
            case VerificationResult.Valid valid:
                var verifiableEvent = new VerifiableEvent(new EventId(request.FederatedStreamId.StreamId, nextEventIndex), request.SignedEvent.Serialize());
                await batcher.PublishEvent(verifiableEvent);
                return new CommandStepResult(request.CommandStepId, CommandStepState.Completed);

            case VerificationResult.Invalid invalid:
                return new CommandStepResult(request.CommandStepId, CommandStepState.Failed, invalid.ErrorMessage);

            default:
                throw new NotImplementedException($"Unsupporetd VerificationResult type ”{result.GetType().Name}”");
        }
    }
}
