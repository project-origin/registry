using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Verifiers;

internal class ProductionTransferredVerifier : IEventVerifier<ProductionCertificate, V1.TransferredEvent>
{
    public Task<VerificationResult> Verify(VerificationRequest<ProductionCertificate, V1.TransferredEvent> request)
    {
        if (request.Model is null)
            return new VerificationResult.Invalid("Certificate does not exist");

        var certificateSlice = request.Model.GetCertificateSlice(request.Event.SourceSlice);
        if (certificateSlice is null)
            return new VerificationResult.Invalid("Slice not found");

        if (!Ed25519.Ed25519.Verify(certificateSlice.Owner, request.Event.ToByteArray(), request.Signature))
            return new VerificationResult.Invalid($"Invalid signature for slice");

        if (!PublicKey.TryImport(SignatureAlgorithm.Ed25519, request.Event.NewOwner.Content.ToByteArray(), KeyBlobFormat.RawPublicKey, out _))
            return new VerificationResult.Invalid("Invalid NewOwner key, not a valid Ed25519 publicKey");

        return new VerificationResult.Valid();
    }
}
