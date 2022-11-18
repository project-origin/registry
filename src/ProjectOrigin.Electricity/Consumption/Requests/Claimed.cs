using NSec.Cryptography;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Electricity.Consumption.Requests;

internal class ConsumptionClaimedVerifier : ICommandStepVerifier<V1.ClaimCommand.Types.ClaimedEvent, ConsumptionCertificate>
{
    private IModelLoader loader;

    public ConsumptionClaimedVerifier(IModelLoader loader)
    {
        this.loader = loader;
    }

    public async Task<VerificationResult> Verify(CommandStep<V1.ClaimCommand.Types.ClaimedEvent> commandStep, ConsumptionCertificate? model)
    {
        if (model is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var allocationId = commandStep.SignedEvent.Event.AllocationId.ToGuid();

        var slice = model.GetAllocation(allocationId);
        if (slice is null)
            return new VerificationResult.Invalid("Allocation does not exist");

        if (!commandStep.SignedEvent.VerifySignature(slice.Owner))
            return new VerificationResult.Invalid($"Invalid signature");

        var (productionCertificate, _) = await loader.Get<ProductionCertificate>(slice.ProductionCertificateId);
        if (productionCertificate == null || !productionCertificate.HasClaim(allocationId))
            return new VerificationResult.Invalid("Production not claimed");

        return new VerificationResult.Valid();
    }
}
