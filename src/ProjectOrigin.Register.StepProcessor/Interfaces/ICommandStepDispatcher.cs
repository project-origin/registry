using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Register.StepProcessor.Interfaces;

public interface ICommandStepVerifiere
{
    Task<VerificationResult> Verify(V1.CommandStep request, IDictionary<V1.FederatedStreamId, IEnumerable<V1.SignedEvent>> streams);
}
