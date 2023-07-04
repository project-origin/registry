using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;
using System.Linq;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Server.Interfaces;

namespace ProjectOrigin.Electricity.Server.Verifiers;

public class SlicedEventVerifier : IEventVerifier<V1.SlicedEvent>
{
    public Task<VerificationResult> Verify(Transaction transaction, GranularCertificate? productionCertificate, V1.SlicedEvent payload)
    {
        if (productionCertificate is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var certificateSlice = productionCertificate.GetCertificateSlice(payload.SourceSliceHash);
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
                certificateSlice.Commitment.ToModel(),
                payload.NewSlices.Select(slice => slice.Quantity.ToModel()).Aggregate((left, right) => left + right),
                payload.CertificateId.StreamId.Value))
            return new VerificationResult.Invalid($"Invalid sum proof");

        return new VerificationResult.Valid();
    }
}
