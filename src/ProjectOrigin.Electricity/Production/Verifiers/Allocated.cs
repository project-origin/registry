using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Verifiers;

internal class ProductionAllocatedEventVerifier : IEventVerifier<ProductionCertificate, V1.AllocatedEvent>
{
    public Task<VerificationResult> Verify(VerificationRequest<ProductionCertificate, AllocatedEvent> request)
    {
        if (request.Model is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var productionSlice = request.Model.GetCertificateSlice(request.Event.ProductionSourceSlice);
        if (productionSlice is null)
            return new VerificationResult.Invalid("Production slice does not exist");

        if (!Ed25519.Ed25519.Verify(productionSlice.Owner, request.Event.ToByteArray(), request.Signature))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        var consumptionCertificate = request.GetModel<ConsumptionCertificate>(request.Event.ConsumptionCertificateId);
        if (consumptionCertificate == null)
            return new VerificationResult.Invalid("ConsumptionCertificate does not exist");

        if (consumptionCertificate.Period != request.Model.Period)
            return new VerificationResult.Invalid("Certificates are not in the same period");

        if (consumptionCertificate.GridArea != request.Model.GridArea)
            return new VerificationResult.Invalid("Certificates are not in the same area");

        var consumptionSlice = consumptionCertificate.GetCertificateSlice(request.Event.ConsumptionSourceSlice);
        if (consumptionSlice is null)
            return new VerificationResult.Invalid("Consumption slice does not exist");

        if (!Group.Default.VerifyEqualityProof(request.Event.EqualityProof.ToByteArray(), productionSlice.Commitment, consumptionSlice.Commitment))
            return new VerificationResult.Invalid("Invalid Equality proof");

        return new VerificationResult.Valid();
    }
}
