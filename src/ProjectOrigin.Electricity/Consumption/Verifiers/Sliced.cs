using Google.Protobuf;
using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.Utils;
using ProjectOrigin.Registry.V1;

namespace ProjectOrigin.Electricity.Consumption.Verifiers;

public class ConsumptionSlicedVerifier : IEventVerifier<ConsumptionCertificate, V1.SlicedEvent>
{
    private IKeyAlgorithm _keyAlgorithm;

    public ConsumptionSlicedVerifier(IKeyAlgorithm keyAlgorithm)
    {
        _keyAlgorithm = keyAlgorithm;
    }

    public Task<VerificationResult> Verify(Transaction transaction, ConsumptionCertificate? certificate, V1.SlicedEvent payload)
    {
        if (certificate is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var certificateSlice = certificate.GetCertificateSlice(payload.SourceSlice);
        if (certificateSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        if (!certificateSlice.Owner.VerifySignature(transaction.Header.ToByteArray(), transaction.HeaderSignature))
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
