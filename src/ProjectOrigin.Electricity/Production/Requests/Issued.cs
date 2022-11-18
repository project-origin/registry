using Google.Protobuf;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Requests;

internal class ProductionIssuedVerifier : ICommandStepVerifier<V1.IssueProductionCommand.Types.ProductionIssuedEvent, ProductionCertificate>
{
    private IssuerOptions issuerOptions;

    public ProductionIssuedVerifier(IOptions<IssuerOptions> issuerOptions)
    {
        this.issuerOptions = issuerOptions.Value;
    }

    public Task<VerificationResult> Verify(CommandStep<V1.IssueProductionCommand.Types.ProductionIssuedEvent> commandStep, ProductionCertificate? model)
    {
        var @event = commandStep.SignedEvent.Event;

        var proof = commandStep.Proof as V1.IssueProductionCommand.Types.ProductionIssuedProof;
        if (proof is null)
            return new VerificationResult.Invalid($"Missing or invalid proof");

        if (model is not null)
            return new VerificationResult.Invalid($"Certificate with id ”{commandStep.FederatedStreamId.StreamId}” already exists");

        if (!proof.GsrnProof.Verify(@event.GsrnCommitment))
            return new VerificationResult.Invalid("Calculated GSRN commitment does not equal the parameters");

        if (!proof.QuantityProof.Verify(@event.QuantityCommitment))
            return new VerificationResult.Invalid("Calculated Quantity commitment does not equal the parameters");

        if (@event.QuantityProof is not null
            && !@event.QuantityProof.Equals(proof.QuantityProof))
            return new VerificationResult.Invalid($"Private and public quantity proof does not match");

        if (!PublicKey.TryImport(SignatureAlgorithm.Ed25519, @event.OwnerPublicKey.Content.ToByteArray(), KeyBlobFormat.RawPublicKey, out _))
            return new VerificationResult.Invalid("Invalid owner key, not a valid Ed25519 publicKey");

        var publicKey = issuerOptions.AreaIssuerPublicKey(@event.GridArea);
        if (publicKey is null)
            return new VerificationResult.Invalid($"No issuer found for GridArea ”{@event.GridArea}”");

        if (!commandStep.SignedEvent.VerifySignature(publicKey))
            return new VerificationResult.Invalid($"Invalid issuer signature for GridArea ”{@event.GridArea}”");

        return new VerificationResult.Valid();
    }
}
