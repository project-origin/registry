using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Consumption.Verifiers;

internal class ConsumptionAllocatedVerifier : IEventVerifier<ConsumptionCertificate, V1.AllocatedEvent>
{
    public Task<VerificationResult> Verify(Register.StepProcessor.Interfaces.VerificationRequest<AllocatedEvent> request)
    {
        if (!request.TryGetModel<ConsumptionCertificate>(request.Event.ConsumptionCertificateId, out var consumptionCertificate))
            return new VerificationResult.Invalid("Certificate does not exist");

        var consumptionSlice = consumptionCertificate.GetCertificateSlice(request.Event.ConsumptionSourceSlice);
        if (consumptionSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        if (!Ed25519.Ed25519.Verify(consumptionSlice.Owner, request.Event.ToByteArray(), request.Signature))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        if (!request.TryGetModel<ProductionCertificate>(request.Event.ProductionCertificateId, out var productionCertificate))
            return new VerificationResult.Invalid("ProductionCertificate does not exist");

        if (!productionCertificate.HasAllocation(request.Event.AllocationId))
            return new VerificationResult.Invalid("Production not allocated");

        return new VerificationResult.Valid();
    }
}
