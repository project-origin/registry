using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Register.StepProcessor.Interfaces;

public interface ICommandStepVerifier
{
    Task<VerificationResult> Verify(V1.CommandStep request, Dictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>> streams);
}
