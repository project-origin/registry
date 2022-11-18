using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Electricity.Consumption.Requests;

internal class ConsumptionIssuedVerifier : ICommandStepVerifier<V1.IssueConsumptionCommand.Types.ConsumptionIssuedEvent, ConsumptionCertificate>
{
    private IssuerOptions issuerOptions;

    public ConsumptionIssuedVerifier(IOptions<IssuerOptions> issuerOptions)
    {
        this.issuerOptions = issuerOptions.Value;
    }

    public Task<VerificationResult> Verify(CommandStep<V1.IssueConsumptionCommand.Types.ConsumptionIssuedEvent> commandStep, ConsumptionCertificate? model)
    {
        var @event = commandStep.SignedEvent.Event;

        if (model != null)
            return new VerificationResult.Invalid($"Certificate with id ”{commandStep.FederatedStreamId.StreamId}” already exists");

        var proof = commandStep.Proof as V1.IssueConsumptionCommand.Types.ConsumptionIssuedProof;
        if (proof is null)
            return new VerificationResult.Invalid($"Missing or invalid proof");

        if (!proof.GsrnProof.Verify(@event.GsrnCommitment))
            return new VerificationResult.Invalid("Calculated GSRN commitment does not equal the parameters");

        if (!proof.QuantityProof.Verify(@event.QuantityCommitment))
            return new VerificationResult.Invalid("Calculated Quantity commitment does not equal the parameters");

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
