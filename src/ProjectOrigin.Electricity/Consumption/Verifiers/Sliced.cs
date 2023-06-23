using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Verifier.Utils;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;
using System.Linq;

namespace ProjectOrigin.Electricity.Consumption.Verifiers;

public class ConsumptionSlicedVerifier : IEventVerifier<ConsumptionCertificate, V1.SlicedEvent>
{
    public Task<VerificationResult> Verify(Transaction transaction, ConsumptionCertificate? certificate, V1.SlicedEvent payload)
    {
        if (certificate is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var certificateSlice = certificate.GetCertificateSlice(payload.SourceSliceHash);
        if (certificateSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        if (!transaction.IsSignatureValid(certificateSlice.Owner))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        foreach (var slice in payload.NewSlices)
        {
            if (!slice.NewOwner.TryToModel(out _))
                return new VerificationResult.Invalid("Invalid NewOwner key, not a valid publicKey");

            if (!slice.Quantity.VerifyCommitment(payload.CertificateId.StreamId.Value))
                return new VerificationResult.Invalid("Invalid range proof for Quantity commitment");
        }

        if (!Commitment.VerifyEqualityProof(
            payload.SumProof.ToByteArray(),
            certificateSlice.Commitment,
            payload.NewSlices.Select(slice => slice.Quantity.ToModel()).Aggregate((left, right) => left + right),
            payload.CertificateId.StreamId.Value))
            return new VerificationResult.Invalid($"Invalid sum proof");

        return new VerificationResult.Valid();
    }
}
