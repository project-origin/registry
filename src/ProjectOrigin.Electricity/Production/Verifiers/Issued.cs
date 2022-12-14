using Google.Protobuf;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Verifiers;

internal class ProductionIssuedVerifier : IEventVerifier<V1.ProductionIssuedEvent>
{
    private IssuerOptions _issuerOptions;

    public ProductionIssuedVerifier(IOptions<IssuerOptions> issuerOptions)
    {
        _issuerOptions = issuerOptions.Value;
    }

    public Task<VerificationResult> Verify(Register.StepProcessor.Interfaces.VerificationRequest<V1.ProductionIssuedEvent> request)
    {
        if (request.TryGetModel<ProductionCertificate>(request.Event.CertificateId, out _))
            return new VerificationResult.Invalid($"Certificate with id ”{request.Event.CertificateId.StreamId}” already exists");

        if (!request.Event.GsrnCommitment.VerifyCommitment())
            return new VerificationResult.Invalid("Invalid range proof forr GSRN commitment");

        if (!request.Event.QuantityCommitment.VerifyCommitment())
            return new VerificationResult.Invalid("Invalid range proof forr Quantity commitment");

        if (request.Event.QuantityPublication is not null
            && !request.Event.QuantityCommitment.VerifyPublication(request.Event.QuantityPublication))
            return new VerificationResult.Invalid($"Private and public quantity proof does not match");

        if (!PublicKey.TryImport(SignatureAlgorithm.Ed25519, request.Event.OwnerPublicKey.Content.ToByteArray(), KeyBlobFormat.RawPublicKey, out _))
            return new VerificationResult.Invalid("Invalid owner key, not a valid Ed25519 publicKey");

        var publicKey = _issuerOptions.AreaIssuerPublicKey(request.Event.GridArea);
        if (publicKey is null)
            return new VerificationResult.Invalid($"No issuer found for GridArea ”{request.Event.GridArea}”");

        if (!Ed25519.Ed25519.Verify(publicKey, request.Event.ToByteArray(), request.Signature))
            return new VerificationResult.Invalid($"Invalid issuer signature for GridArea ”{request.Event.GridArea}”");

        return new VerificationResult.Valid();
    }
}
