using Google.Protobuf;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Consumption.Verifiers;


internal class ConsumptionIssuedVerifier : IEventVerifier<ConsumptionCertificate, V1.ConsumptionIssuedEvent>
{
    private IssuerOptions _issuerOptions;

    public ConsumptionIssuedVerifier(IOptions<IssuerOptions> issuerOptions)
    {
        _issuerOptions = issuerOptions.Value;
    }

    public Task<VerificationResult> Verify(VerificationRequest<ConsumptionCertificate, V1.ConsumptionIssuedEvent> request)
    {
        if (request.Model is not null)
            return new VerificationResult.Invalid($"Certificate with id ”{request.Event.CertificateId.StreamId}” already exists");

        if (!request.Event.GsrnCommitment.VerifyCommitment())
            return new VerificationResult.Invalid("Invalid range proof forr GSRN commitment");

        if (!request.Event.QuantityCommitment.VerifyCommitment())
            return new VerificationResult.Invalid("Invalid range proof forr Quantity commitment");

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
