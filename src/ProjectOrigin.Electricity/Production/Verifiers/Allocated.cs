using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Requests;

internal class ProductionAllocatedEventVerifier : IEventVerifier<ProductionCertificate, V1.AllocatedEvent>
{
    public Task<VerificationResult> Verify(VerificationRequest<ProductionCertificate, AllocatedEvent> request)
    {
        var hydrator = new ModelHydrater();

        if (request.Model is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var productionSlice = request.Model.GetCertificateSlice(request.Event.ProductionSourceSlice);
        if (productionSlice is null)
            return new VerificationResult.Invalid("Production slice does not exist");

        if (!Ed25519.Ed25519.Verify(productionSlice.Owner, request.Event.ToByteArray(), request.Signature))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        if (!request.AdditionalStreams.TryGetValue(request.Event.ConsumptionCertificateId, out var events))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        var consumptionCertificate = hydrator.HydrateModel<ConsumptionCertificate>(events);
        if (consumptionCertificate == null)
            return new VerificationResult.Invalid("ConsumptionCertificate does not exist");

        if (consumptionCertificate.Period != request.Model.Period)
            return new VerificationResult.Invalid("Certificates are not in the same period");

        if (consumptionCertificate.GridArea != request.Model.GridArea)
            return new VerificationResult.Invalid("Certificates are not in the same area");

        var consumptionSlice = request.Model.GetCertificateSlice(request.Event.ConsumptionSourceSlice);
        if (consumptionSlice is null)
            return new VerificationResult.Invalid("consumption slice does not exist");

        if (!Group.Default.VerifyEqualityProof(request.Event.EqualityProof.ToByteArray(), productionSlice.Commitment, consumptionSlice.Commitment))
            return new VerificationResult.Invalid("Certificates are not in the same area");

        return new VerificationResult.Valid();
    }
}
