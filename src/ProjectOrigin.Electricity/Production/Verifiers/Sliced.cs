using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Verifier.Utils;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;
using System.Linq;

namespace ProjectOrigin.Electricity.Production.Verifiers;

public class ProductionSlicedVerifier : IEventVerifier<ProductionCertificate, V1.SlicedEvent>
{
    private IHDAlgorithm _keyAlgorithm;

    public ProductionSlicedVerifier(IHDAlgorithm keyAlgorithm)
    {
        _keyAlgorithm = keyAlgorithm;
    }

    public Task<VerificationResult> Verify(Transaction transaction, ProductionCertificate? productionCertificate, V1.SlicedEvent payload)
    {
        if (productionCertificate is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var certificateSlice = productionCertificate.GetCertificateSlice(payload.SourceSlice);
        if (certificateSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        if (!transaction.IsSignatureValid(certificateSlice.Owner))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        foreach (var slice in payload.NewSlices)
        {
            if (!_keyAlgorithm.TryImport(slice.NewOwner.Content.Span, out _))
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
