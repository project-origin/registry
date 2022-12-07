using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Interfaces;

public interface IEventVerifier<TModel, TEvent>
{
    Task<VerificationResult> Verify(VerificationRequest<TModel, TEvent> request);
}

public record VerificationRequest<TModel, TEvent>(
    TModel? Model,
    TEvent Event,
    byte[] Signature,
    Dictionary<Register.V1.FederatedStreamId, IEnumerable<Register.V1.SignedEvent>> AdditionalStreams);
