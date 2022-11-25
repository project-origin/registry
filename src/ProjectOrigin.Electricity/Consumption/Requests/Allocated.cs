using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Consumption.Requests;

public class ConsumptionAllocatedVerifier : ICommandStepVerifier<V1.ClaimCommand.Types.AllocatedEvent, ConsumptionCertificate>
{
    private IModelLoader _loader;

    public ConsumptionAllocatedVerifier(IModelLoader loader)
    {
        _loader = loader;
    }

    public async Task<VerificationResult> Verify(CommandStep<V1.ClaimCommand.Types.AllocatedEvent> commandStep, ConsumptionCertificate? model)
    {
        var @event = commandStep.SignedEvent.Event;

        if (model is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var proof = commandStep.Proof as V1.SliceProof;
        if (proof is null)
            return new VerificationResult.Invalid($"Missing or invalid proof");

        var certificateSlice = model.GetCertificateSlice(@event.Slice.ToModel());
        if (certificateSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        var verificationResult = certificateSlice.Verify(commandStep.SignedEvent, proof.ToModel(), @event.Slice.ToModel());
        if (verificationResult is VerificationResult.Invalid)
            return verificationResult;

        var allocationId = @event.AllocationId.ToModel();

        var (productionCertificate, _) = await _loader.Get<ProductionCertificate>(@event.ProductionCertificateId.ToModel());
        if (productionCertificate == null
            || !productionCertificate.HasAllocation(allocationId))
            return new VerificationResult.Invalid("Production not allocated");

        if (productionCertificate.GetAllocation(allocationId)!.Commitment != @event.Slice.Quantity.ToModel())
            return new VerificationResult.Invalid("Commmitment are not the same");

        return new VerificationResult.Valid();
    }
}
