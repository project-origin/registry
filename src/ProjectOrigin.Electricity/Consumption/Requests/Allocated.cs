using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Consumption.Requests;

internal class ConsumptionAllocatedVerifier : IEventVerifier<ConsumptionCertificate, V1.AllocatedEvent>
{
    public Task<VerificationResult> Verify(VerificationRequest<ConsumptionCertificate, AllocatedEvent> request)
    {
        var hydrator = new ModelHydrater();

        if (request.Model is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var consumptionSlice = request.Model.GetCertificateSlice(request.Event.ConsumptionSourceSlice);
        if (consumptionSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        if (!Ed25519.Ed25519.Verify(consumptionSlice.Owner, request.Event.ToByteArray(), request.Signature))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        if (!request.AdditionalStreams.TryGetValue(request.Event.ProductionCertificateId, out var events))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        var productionCertificate = hydrator.HydrateModel<ProductionCertificate>(events);
        if (productionCertificate == null)
            return new VerificationResult.Invalid("ProductionCertificate does not exist");

        var allocationId = request.Event.AllocationId.ToModel();

        if (productionCertificate.HasAllocation(allocationId))
            return new VerificationResult.Invalid("Consumption not allocated");

        return new VerificationResult.Valid();
    }
}
