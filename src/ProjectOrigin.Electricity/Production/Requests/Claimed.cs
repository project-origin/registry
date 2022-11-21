using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Requests;

internal class ProductionClaimedVerifier : ICommandStepVerifier<V1.ClaimCommand.Types.ClaimedEvent, ProductionCertificate>
{
    private IModelLoader loader;

    public ProductionClaimedVerifier(IModelLoader loader)
    {
        this.loader = loader;
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

        var (consumptionCertificate, _) = await loader.Get<ConsumptionCertificate>(slice.ConsumptionCertificateId);
        if (consumptionCertificate == null || !consumptionCertificate.HasAllocation(allocationId))
            return new VerificationResult.Invalid("Consumption not allocated");

        return new VerificationResult.Valid();
    }
}
