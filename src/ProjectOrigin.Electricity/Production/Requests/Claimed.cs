using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Requests;

public class ProductionClaimedVerifier : ICommandStepVerifier<V1.ClaimCommand.Types.ClaimedEvent, ProductionCertificate>
{
    private IModelLoader _loader;

    public ProductionClaimedVerifier(IModelLoader loader)
    {
        _loader = loader;
    }

    public async Task<VerificationResult> Verify(CommandStep<V1.ClaimCommand.Types.ClaimedEvent> commandStep, ProductionCertificate? model)
    {
        if (model is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var allocationId = commandStep.SignedEvent.Event.AllocationId.ToModel();

        var slice = model.GetAllocation(allocationId);
        if (slice is null)
            return new VerificationResult.Invalid("Allocation does not exist");

        if (!commandStep.SignedEvent.VerifySignature(slice.Owner))
            return new VerificationResult.Invalid($"Invalid signature");

        var (consumptionCertificate, _) = await _loader.Get<ConsumptionCertificate>(slice.ConsumptionCertificateId);
        if (consumptionCertificate == null || !consumptionCertificate.HasAllocation(allocationId))
            return new VerificationResult.Invalid("Consumption not allocated");

        return new VerificationResult.Valid();
    }
}
