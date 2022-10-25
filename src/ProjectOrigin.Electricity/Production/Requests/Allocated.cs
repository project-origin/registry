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

        var sliceFound = model.GetSlice(request.Event.Slice.Source);
        var verificationResult = VerifySlice(request, request.SliceParameters, request.Event.Slice, sliceFound);
        if (!verificationResult.IsValid)
            return verificationResult;

        var (consumptionCertificate, _) = await loader.Get<ConsumptionCertificate>(request.Event.ConsumptionCertificateId);
        if (consumptionCertificate == null)
            return VerificationResult.Invalid("ConsumptionCertificate does not exist");

        if (consumptionCertificate.Period != model.Period)
            return VerificationResult.Invalid("Certificates are not in the same period");

        if (consumptionCertificate.GridArea != model.GridArea)
            return VerificationResult.Invalid("Certificates are not in the same area");

        return VerificationResult.Valid;
    }
}
