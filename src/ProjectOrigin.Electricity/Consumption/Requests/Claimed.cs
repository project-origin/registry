using ProjectOrigin.Electricity.Shared;
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

    public ConsumptionClaimedVerifier(IEventSerializer serializer)
    {
        this.serializer = serializer;
    }

    public Task<VerificationResult> Verify(ConsumptionClaimedRequest request, ConsumptionCertificate? model)
    {
        if (model is null)
            return VerificationResult.Invalid("Certificate does not exist");

        throw new NotImplementedException("Verify allocation exists");
        throw new NotImplementedException("Verify production allocated!");
        throw new NotImplementedException("Verify signature");
    }
}
