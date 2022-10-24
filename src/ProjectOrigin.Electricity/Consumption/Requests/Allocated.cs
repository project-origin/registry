using ProjectOrigin.Electricity.Shared;
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
    public ConsumptionAllocatedVerifier(IEventSerializer serializer) : base(serializer)
    {
    }

    public Task<VerificationResult> Verify(ConsumptionAllocatedRequest request, ConsumptionCertificate? model)
    {
        if (model is null)
            return VerificationResult.Invalid("Certificate does not exist");

        throw new NotImplementedException("Verify production allocated!");

        return VerifySlice(request, request.SliceParameters, request.Event.Slice, model.Slices);
    }
}

// public static class AllocatedConsumptionHttpClassExtensions
// {
//     static IEventSerializer serializer = new JsonEventSerializer();

//     public static void AllocatedConsumption(this HttpClient client,
//         Guid allocationId,
//         CertifcateId productionCertificateId,
//         CertifcateId consumptionCertificateId,
//         CommitmentParameters source,
//         CommitmentParameters quantity,
//         CommitmentParameters remainder,
//         Key ownerKey)
//     {
//         var e = new ConsumptionAllocated(
//                 allocationId,
//                 productionCertificateId,
//                 consumptionCertificateId,
//                 new Slice(source.Commitment, quantity.Commitment, remainder.Commitment, 0)
//             );

//         var serializedEvent = serializer.Serialize(e);
//         var signature = NSec.Cryptography.Ed25519.Ed25519.Sign(ownerKey, serializedEvent);

//         var rerquest = new ConsumptionAllocatedRequest(
//             new(
//                 source, quantity, remainder
//             ),
//             e,
//             signature
//         );

//         throw new NotImplementedException();
//     }
// }
