using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Verifiers;

internal class ProductionClaimedVerifier : IEventVerifier<ProductionCertificate, V1.ClaimedEvent>
{
    public Task<VerificationResult> Verify(VerificationRequest<ProductionCertificate, ClaimedEvent> request)
    {
        if (request.Model is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var slice = request.Model.GetAllocation(request.Event.AllocationId);
        if (slice is null)
            return new VerificationResult.Invalid("Allocation does not exist");

        if (!Ed25519.Ed25519.Verify(slice.Owner, request.Event.ToByteArray(), request.Signature))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        var consumptionCertificate = request.GetModel<ConsumptionCertificate>(slice.ConsumptionCertificateId);
        if (consumptionCertificate == null)
            return new VerificationResult.Invalid("ConsumptionCertificate does not exist");

        if (!consumptionCertificate.HasAllocation(request.Event.AllocationId))
            return new VerificationResult.Invalid("Consumption not allocated");

        return new VerificationResult.Valid();
    }
}
