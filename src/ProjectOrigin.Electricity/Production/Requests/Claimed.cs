using ProjectOrigin.Electricity.Shared;
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

    public ProductionClaimedVerifier(IEventSerializer serializer)
    {
        this.serializer = serializer;
    }

    public Task<VerificationResult> Verify(ProductionClaimedRequest request, ProductionCertificate? model)
    {
        if (model is null)
            return VerificationResult.Invalid("Certificate does not exist");

        throw new NotImplementedException("Verify allocation exists");
        throw new NotImplementedException("Verify consumption claimed!");
        throw new NotImplementedException("Verify signature");
    }
}
