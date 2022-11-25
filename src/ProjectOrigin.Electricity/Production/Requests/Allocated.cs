using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Requests;

public class ProductionAllocatedVerifier : ICommandStepVerifier<V1.ClaimCommand.Types.AllocatedEvent, ProductionCertificate>
{
    private IModelLoader _loader;

    public ProductionAllocatedVerifier(IModelLoader loader)
    {
        _loader = loader;
    }

    public async Task<VerificationResult> Verify(CommandStep<V1.ClaimCommand.Types.AllocatedEvent> commandStep, ProductionCertificate? model)
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

        var (consumptionCertificate, _) = await _loader.Get<ConsumptionCertificate>(@event.ConsumptionCertificateId.ToModel());
        if (consumptionCertificate == null)
            return new VerificationResult.Invalid("ConsumptionCertificate does not exist");

        if (consumptionCertificate.Period != model.Period)
            return new VerificationResult.Invalid("Certificates are not in the same period");

        if (consumptionCertificate.GridArea != model.GridArea)
            return new VerificationResult.Invalid("Certificates are not in the same area");

        return new VerificationResult.Valid();
    }
}
