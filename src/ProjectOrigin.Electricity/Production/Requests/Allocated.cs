using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Requests;

internal record ProductionAllocatedEvent(
    Guid AllocationId,
    FederatedStreamId ProductionCertificateId,
    FederatedStreamId ConsumptionCertificateId,
    Slice Slice);

internal record ProductionAllocatedRequest(
    SliceParameters SliceParameters,
    ProductionAllocatedEvent Event,
    byte[] Signature
    ) : PublishRequest<ProductionAllocatedEvent>(Event.ProductionCertificateId, Signature, Event);

internal class ProductionAllocatedVerifier : SliceVerifier, IRequestVerifier<ProductionAllocatedRequest, ProductionCertificate>
{
    private IModelLoader loader;

    public ProductionAllocatedVerifier(IEventSerializer serializer, IModelLoader loader) : base(serializer)
    {
        this.loader = loader;
    }

    public async Task<VerificationResult> Verify(ProductionAllocatedRequest request, ProductionCertificate? model)
    {
        if (model is null)
            return VerificationResult.Invalid("Certificate does not exist");

        var (consumptionCertificate, _) = await loader.Get<ConsumptionCertificate>(request.Event.ConsumptionCertificateId);
        if (consumptionCertificate == null)
            return VerificationResult.Invalid("ConsumptionCertificate does not exist");

        return VerifySlice(request, request.SliceParameters, request.Event.Slice, model.AvailableSlices);
    }
}
