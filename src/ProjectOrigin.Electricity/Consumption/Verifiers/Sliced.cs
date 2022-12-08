using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Verifiers;

internal class ConsumptionSlicedEventVerifier : IEventVerifier<ConsumptionCertificate, V1.SlicedEvent>
{
    public Task<VerificationResult> Verify(VerificationRequest<ConsumptionCertificate, V1.SlicedEvent> request)
    {
        if (request.Model is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var certificateSlice = request.Model.GetCertificateSlice(request.Event.SourceSlice);
        if (certificateSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        if (!Ed25519.Ed25519.Verify(certificateSlice.Owner, request.Event.ToByteArray(), request.Signature))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        foreach (var slice in request.Event.NewSlices)
        {
            if (!PublicKey.TryImport(SignatureAlgorithm.Ed25519, slice.NewOwner.Content.ToByteArray(), KeyBlobFormat.RawPublicKey, out _))
                return new VerificationResult.Invalid("Invalid NewOwner key, not a valid Ed25519 publicKey");
        }

        if (!Group.Default.VerifyEqualityProof(
            request.Event.SumProof.ToByteArray(),
            certificateSlice.Commitment,
            request.Event.NewSlices.Select(slice => slice.Quantity.ToModel()).Aggregate((b, c) => b * c)))
            return new VerificationResult.Invalid($"Invalid sum proof");

        return new VerificationResult.Valid();
    }
}
