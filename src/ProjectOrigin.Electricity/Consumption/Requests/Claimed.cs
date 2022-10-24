using NSec.Cryptography;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.Electricity.Consumption.Requests;

internal record ConsumptionClaimedEvent(
    FederatedStreamId Id,
    Guid AllocationId);

internal record ConsumptionClaimedRequest(
    ConsumptionClaimedEvent Event,
    byte[] Signature
    ) : PublishRequest<ConsumptionClaimedEvent>(Event.Id, Signature, Event);

internal class ConsumptionClaimedVerifier : IRequestVerifier<ConsumptionClaimedRequest, ConsumptionCertificate>
{
    private IEventSerializer serializer;
    private IModelLoader loader;

    public ConsumptionClaimedVerifier(IEventSerializer serializer, IModelLoader loader)
    {
        this.serializer = serializer;
        this.loader = loader;
    }

    public async Task<VerificationResult> Verify(ConsumptionClaimedRequest request, ConsumptionCertificate? model)
    {
        if (model is null)
            return VerificationResult.Invalid("Certificate does not exist");

        var slice = model.AllocationSlices.Single(x => x.AllocationId == request.Event.AllocationId);
        if (slice is null)
            return VerificationResult.Invalid("Allocation does not exist");

        var data = serializer.Serialize(request.Event);
        if (!Ed25519.Ed25519.Verify(slice.Owner, data, request.Signature))
            return VerificationResult.Invalid($"Invalid signature");

        var (productionCertificate, _) = await loader.Get<ProductionCertificate>(slice.ProductionCertificateId);
        if (productionCertificate == null || productionCertificate.HasClaim(request.Event.AllocationId))
            throw new NotImplementedException("Verify production not claimed");

        return VerificationResult.Valid;
    }
}
