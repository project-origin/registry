using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Shared;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.Electricity.Consumption.Requests;

internal record ConsumptionIssuedEvent(
    FederatedStreamId CertificateId,
    TimePeriod Period,
    string GridArea,
    Commitment GsrnCommitment,
    Commitment QuantityCommitment,
    byte[] OwnerPublicKey);

internal record ConsumptionIssuedRequest(
    CommitmentParameters GsrnParameters,
    CommitmentParameters QuantityParameters,
    ConsumptionIssuedEvent Event,
    byte[] Signature
    ) : PublishRequest<ConsumptionIssuedEvent>(Event.CertificateId, Signature, Event);

internal class ConsumptionIssuedVerifier : IRequestVerifier<ConsumptionIssuedRequest, ConsumptionCertificate>
{
    private IssuerOptions issuerOptions;
    private IEventSerializer serializer;

    public ConsumptionIssuedVerifier(IOptions<IssuerOptions> issuerOptions, IEventSerializer serializer)
    {
        this.issuerOptions = issuerOptions.Value;
        this.serializer = serializer;
    }

    public Task<VerificationResult> Verify(ConsumptionIssuedRequest request, ConsumptionCertificate? model)
    {
        if (model != null)
            return VerificationResult.Invalid($"Certificate with id ”{request.FederatedStreamId.StreamId}” already exists");

        if (!request.GsrnParameters.Verify(request.Event.GsrnCommitment))
            return VerificationResult.Invalid("Calculated GSRN commitment does not equal the parameters");

        if (!request.QuantityParameters.Verify(request.Event.QuantityCommitment))
            return VerificationResult.Invalid("Calculated Quantity commitment does not equal the parameters");

        if (!PublicKey.TryImport(SignatureAlgorithm.Ed25519, request.Event.OwnerPublicKey, KeyBlobFormat.RawPublicKey, out _))
            return VerificationResult.Invalid("Invalid owner key, not a valid Ed25519 publicKey");

        var publicKey = issuerOptions.AreaIssuerPublicKey(request.Event.GridArea);
        if (publicKey is null)
            return VerificationResult.Invalid($"No issuer found for GridArea ”{request.Event.GridArea}”");

        var data = serializer.Serialize(request.Event);
        if (!Ed25519.Ed25519.Verify(publicKey, data, request.Signature))
            return VerificationResult.Invalid($"Invalid issuer signature for GridArea ”{request.Event.GridArea}”");

        return VerificationResult.Valid;
    }
}
