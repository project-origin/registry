using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Verifiers;

internal class ProductionClaimedVerifier : IEventVerifier<ProductionCertificate, V1.ClaimedEvent>
{
    public Task<VerificationResult> Verify(Register.StepProcessor.Interfaces.VerificationRequest<ClaimedEvent> request)
    {
        if (!request.TryGetModel<ProductionCertificate>(request.Event.CertificateId, out var productionCertificate))
            return new VerificationResult.Invalid("Certificate does not exist");

        var slice = productionCertificate.GetAllocation(request.Event.AllocationId);
        if (slice is null)
            return new VerificationResult.Invalid("Allocation does not exist");

        if (!Ed25519.Ed25519.Verify(slice.Owner, request.Event.ToByteArray(), request.Signature))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        if (!request.TryGetModel<ConsumptionCertificate>(slice.ConsumptionCertificateId, out var consumptionCertificate))
            return new VerificationResult.Invalid("ConsumptionCertificate does not exist");

        if (!consumptionCertificate.HasAllocation(request.Event.AllocationId))
            return new VerificationResult.Invalid("Consumption not allocated");

        return new VerificationResult.Valid();
    }
}
