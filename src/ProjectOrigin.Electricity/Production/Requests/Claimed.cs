using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Requests;

internal record ProductionClaimedEvent(
    FederatedStreamId CertificateId,
    Guid AllocationId);

internal record ProductionClaimedRequest(
    ProductionClaimedEvent Event,
    byte[] Signature
    ) : PublishRequest<ProductionClaimedEvent>(Event.CertificateId, Signature, Event);

internal class ProductionClaimedVerifier : IRequestVerifier<ProductionClaimedRequest, ProductionCertificate>
{
    private IEventSerializer serializer;
    private IModelLoader loader;

    public ProductionClaimedVerifier(IEventSerializer serializer, IModelLoader loader)
    {
        this.serializer = serializer;
        this.loader = loader;
    }

    public async Task<VerificationResult> Verify(ProductionClaimedRequest request, ProductionCertificate? model)
    {
        if (model is null)
            return VerificationResult.Invalid("Certificate does not exist");

        var slice = model.GetAllocation(request.Event.AllocationId);
        if (slice is null)
            return VerificationResult.Invalid("Allocation does not exist");

        var data = serializer.Serialize(request.Event);
        if (!Ed25519.Ed25519.Verify(slice.Owner, data, request.Signature))
            return VerificationResult.Invalid($"Invalid signature");

        var (consumptionCertificate, _) = await loader.Get<ConsumptionCertificate>(slice.ConsumptionCertificateId);
        if (consumptionCertificate == null || !consumptionCertificate.HasAllocation(request.Event.AllocationId))
            return VerificationResult.Invalid("Consumption not allocated");

        return VerificationResult.Valid;
    }
}
