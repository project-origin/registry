using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Consumption.Verifiers;

internal class ConsumptionClaimedVerifier : IEventVerifier<ConsumptionCertificate, V1.ClaimedEvent>
{
    public Task<VerificationResult> Verify(Register.StepProcessor.Interfaces.VerificationRequest<ClaimedEvent> request)
    {
        if (!request.TryGetModel<ConsumptionCertificate>(request.Event.CertificateId, out var consumptionCertificate))
            return new VerificationResult.Invalid("Certificate does not exist");

        var slice = consumptionCertificate.GetAllocation(request.Event.AllocationId);
        if (slice is null)
            return new VerificationResult.Invalid("Allocation does not exist");

        if (!Ed25519.Ed25519.Verify(slice.Owner, request.Event.ToByteArray(), request.Signature))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        if (!request.TryGetModel<ProductionCertificate>(slice.ProductionCertificateId, out var productionCertificate))
            return new VerificationResult.Invalid("ProductionCertificate does not exist");

        if (!productionCertificate.HasClaim(request.Event.AllocationId))
            return new VerificationResult.Invalid("Production not claimed");

        return new VerificationResult.Valid();
    }
}
