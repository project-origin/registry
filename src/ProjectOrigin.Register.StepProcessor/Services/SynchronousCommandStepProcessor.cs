using Microsoft.Extensions.Options;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;

namespace ProjectOrigin.Register.StepProcessor.Services;

public class SynchronousCommandStepProcessor : ICommandStepProcessor
{
    private CommandStepProcessorOptions _options;
    private ICommandStepDispatcher _dispatcher;
    private IBatcher _batcher;

    public SynchronousCommandStepProcessor(IOptions<CommandStepProcessorOptions> options, ICommandStepDispatcher dispatcher, IBatcher batcher)
    {
        _options = options.Value;
        _dispatcher = dispatcher;
        _batcher = batcher;
    }

    public async Task<CommandStepResult> Process(CommandStep request)
    {
        if (request.FederatedStreamId.Registry != _options.RegistryName) throw new InvalidDataException("Invalid registry for request");

        var (result, nextEventIndex) = await _dispatcher.Verify(request);

        switch (result)
        {
            case VerificationResult.Valid valid:
                var verifiableEvent = new VerifiableEvent(new EventId(request.FederatedStreamId.StreamId, nextEventIndex), request.SignedEvent.Serialize());
                await _batcher.PublishEvent(verifiableEvent);
                return new CommandStepResult(request.CommandStepId, CommandStepState.Completed);

            case VerificationResult.Invalid invalid:
                return new CommandStepResult(request.CommandStepId, CommandStepState.Failed, invalid.ErrorMessage);

            default:
                throw new NotImplementedException($"Unsupporetd VerificationResult type ”{result.GetType().Name}”");
        }
    }
}
