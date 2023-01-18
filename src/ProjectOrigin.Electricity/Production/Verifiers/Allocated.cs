using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Verifiers;

internal class ProductionAllocatedVerifier : IEventVerifier<ProductionCertificate, V1.AllocatedEvent>
{
    public Task<VerificationResult> Verify(Register.StepProcessor.Interfaces.VerificationRequest<V1.AllocatedEvent> request)
    {
        if (!request.TryGetModel<ProductionCertificate>(request.Event.ProductionCertificateId, out var productionCertificate))
            return new VerificationResult.Invalid("Certificate does not exist");

        var productionSlice = productionCertificate.GetCertificateSlice(request.Event.ProductionSourceSlice);
        if (productionSlice is null)
            return new VerificationResult.Invalid("Production slice does not exist");

        if (!Ed25519.Ed25519.Verify(productionSlice.Owner, request.Event.ToByteArray(), request.Signature))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        if (!request.TryGetModel<ConsumptionCertificate>(request.Event.ConsumptionCertificateId, out var consumptionCertificate))
            return new VerificationResult.Invalid("ConsumptionCertificate does not exist");

        if (consumptionCertificate.Period != productionCertificate.Period)
            return new VerificationResult.Invalid("Certificates are not in the same period");

        if (consumptionCertificate.GridArea != productionCertificate.GridArea)
            return new VerificationResult.Invalid("Certificates are not in the same area");

        var consumptionSlice = consumptionCertificate.GetCertificateSlice(request.Event.ConsumptionSourceSlice);
        if (consumptionSlice is null)
            return new VerificationResult.Invalid("Consumption slice does not exist");

        if (!Commitment.VerifyEqualityProof(
            request.Event.EqualityProof.ToByteArray(),
            productionSlice.Commitment,
            consumptionSlice.Commitment,
            request.Event.AllocationId.Value))
            return new VerificationResult.Invalid("Invalid Equality proof");

        return new VerificationResult.Valid();
    }
}
