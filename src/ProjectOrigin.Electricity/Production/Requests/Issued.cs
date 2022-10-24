using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Shared;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;

namespace ProjectOrigin.Electricity.Production.Requests;

internal record ProductionIssuedEvent(
    FederatedStreamId Id,
    TimePeriod Period,
    string GridArea,
    Commitment GsrnCommitment,
    Commitment QuantityCommitment,
    string FuelCode,
    string TechCode,
    byte[] OwnerPublicKey,
    CommitmentParameters? QuantityParameters = null);


internal record ProductionIssuedRequest(
    CommitmentParameters GsrnCommitmentParameters,
    CommitmentParameters QuantityCommitmentParameters,
    ProductionIssuedEvent Event,
    byte[] Signature
    ) : PublishRequest<ProductionIssuedEvent>(Event.Id, Signature, Event);


internal class ProductionIssuedVerifier : IRequestVerifier<ProductionIssuedRequest, ProductionCertificate>
{
    private IssuerOptions issuerOptions;
    private IEventSerializer serializer;

    public ProductionIssuedVerifier(IOptions<IssuerOptions> issuerOptions, IEventSerializer serializer)
    {
        this.issuerOptions = issuerOptions.Value;
        this.serializer = serializer;
    }

    public Task<VerificationResult> Verify(ProductionIssuedRequest request, ProductionCertificate? model)
    {
        if (model != null)
            return VerificationResult.Invalid($"Certificate with id ”{request.FederatedStreamId.StreamId}” already exists");

        if (!request.GsrnCommitmentParameters.Verify(request.Event.GsrnCommitment))
            return VerificationResult.Invalid("Calculated GSRN commitment does not equal the parameters");

        if (!request.QuantityCommitmentParameters.Verify(request.Event.QuantityCommitment))
            return VerificationResult.Invalid("Calculated Quantity commitment does not equal the parameters");

        if (request.Event.QuantityParameters is not null && request.QuantityCommitmentParameters != request.Event.QuantityParameters)
            return VerificationResult.Invalid($"{nameof(request.Event.QuantityParameters)} and {nameof(request.QuantityCommitmentParameters)} are not the same");

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
