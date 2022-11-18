using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Electricity.Consumption.Requests;

internal class ConsumptionAllocatedVerifier : ICommandStepVerifier<V1.ClaimCommand.Types.AllocatedEvent, ConsumptionCertificate>
{
    private IModelLoader loader;

    public ConsumptionAllocatedVerifier(IModelLoader loader)
    {
        this.loader = loader;
    }

    public async Task<VerificationResult> Verify(CommandStep<V1.ClaimCommand.Types.AllocatedEvent> commandStep, ConsumptionCertificate? model)
    {
        var @event = commandStep.SignedEvent.Event;

        if (model is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var proof = commandStep.Proof as V1.SliceProof;
        if (proof is null)
            return new VerificationResult.Invalid($"Missing or invalid proof");

        var certificateSlice = model.GetCertificateSlice(Slice.From(@event.Slice));
        if (certificateSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        var verificationResult = certificateSlice.Verify(commandStep.SignedEvent, proof, Slice.From(@event.Slice));
        if (verificationResult is VerificationResult.Invalid)
            return verificationResult;

        var allocationId = @event.AllocationId.ToGuid();

        var (productionCertificate, _) = await loader.Get<ProductionCertificate>(@event.ProductionCertificateId);
        if (productionCertificate == null
            || !productionCertificate.HasAllocation(allocationId))
            return new VerificationResult.Invalid("Production not allocated");

        if (productionCertificate.GetAllocation(allocationId)!.Commitment != Mapper.ToModel(@event.Slice.Quantity))
            return new VerificationResult.Invalid("Commmitment are not the same");

        return new VerificationResult.Valid();
    }
}
