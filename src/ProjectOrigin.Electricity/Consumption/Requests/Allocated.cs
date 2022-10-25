using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.Electricity.Consumption.Requests;

internal record ConsumptionAllocatedEvent(
    Guid AllocationId,
    FederatedStreamId ProductionCertificateId,
    FederatedStreamId ConsumptionCertificateId,
    Slice Slice);

internal record ConsumptionAllocatedRequest(
        SliceParameters SliceParameters,
        ConsumptionAllocatedEvent Event,
        byte[] Signature
        ) : PublishRequest<ConsumptionAllocatedEvent>(Event.ConsumptionCertificateId, Signature, Event);

internal class ConsumptionAllocatedVerifier : SliceVerifier, IRequestVerifier<ConsumptionAllocatedRequest, ConsumptionCertificate>
{
    private IModelLoader loader;

    public ConsumptionAllocatedVerifier(IEventSerializer serializer, IModelLoader loader) : base(serializer)
    {
        this.loader = loader;
    }

    public async Task<VerificationResult> Verify(ConsumptionAllocatedRequest request, ConsumptionCertificate? model)
    {
        if (model is null)
            return VerificationResult.Invalid("Certificate does not exist");

        var sliceFound = model.GetSlice(request.Event.Slice.Source);
        var verificationResult = VerifySlice(request, request.SliceParameters, request.Event.Slice, sliceFound);
        if (!verificationResult.IsValid)
            return verificationResult;

        var (productionCertificate, _) = await loader.Get<ProductionCertificate>(request.Event.ProductionCertificateId);
        if (productionCertificate == null || !productionCertificate.HasAllocation(request.Event.AllocationId))
            return VerificationResult.Invalid("Production not allocated");

        if (productionCertificate.GetAllocation(request.Event.AllocationId)!.Commitment != request.Event.Slice.Quantity)
            return VerificationResult.Invalid("Commmitment are not the same");

        return VerificationResult.Valid;
    }
}
