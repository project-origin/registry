using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Consumption.Requests;
using ProjectOrigin.Electricity.Shared;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.RequestProcessor.Services;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionIssuedVerifierTests
{
    private IOptions<T> CreateOptionsMock<T>(T content) where T : class
    {
        var optionsMock = new Mock<IOptions<T>>();
        optionsMock.Setup(obj => obj.Value).Returns(content);
        return optionsMock.Object;
    }

    private Lazy<Group> lazyGroup = new Lazy<Group>(() => Group.Create(), true);

    private (ConsumptionIssuedVerifier, JsonEventSerializer, Key) SetupIssuer()
    {
        var serializer = new JsonEventSerializer();
        var issuerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var optionsMock = CreateOptionsMock(new IssuerOptions((area) => area == "DK1" ? issuerKey.PublicKey : null));

        var processor = new ConsumptionIssuedVerifier(optionsMock, serializer);

        return (processor, serializer, issuerKey);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_IssueCertificate_Success()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var request = CreateRequest(serializer, issuerKey);

        await processor.Verify(request, null);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_CertificateExists_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var request = CreateRequest(serializer, issuerKey);

        var result = await processor.Verify(request, new ConsumptionCertificate());
        Assert.False(result.IsValid);
        Assert.Equal($"Certificate with id ”{request.FederatedStreamId.StreamId}” already exists", result.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_GsrnCommitmentInvalid_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var request = CreateRequest(serializer, issuerKey, gsrnCommitmentOverride: lazyGroup.Value.Commit(57682));

        var result = await processor.Verify(request, null);
        Assert.False(result.IsValid);
        Assert.Equal("Calculated GSRN commitment does not equal the parameters", result.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_QuantityCommitmentInvalid_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var request = CreateRequest(serializer, issuerKey, quantityCommitmentOverride: lazyGroup.Value.Commit(695956));

        var result = await processor.Verify(request, null);
        Assert.False(result.IsValid);
        Assert.Equal("Calculated Quantity commitment does not equal the parameters", result.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_InvalidOwner_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var randomOwnerKeyData = new Fixture().Create<byte[]>();
        var request = CreateRequest(serializer, issuerKey, ownerKeyOverride: randomOwnerKeyData);

        var result = await processor.Verify(request, null);
        Assert.False(result.IsValid);
        Assert.Equal("Invalid owner key, not a valid Ed25519 publicKey", result.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_InvalidSignature_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var someOtherKey = Key.Create(SignatureAlgorithm.Ed25519);

        var request = CreateRequest(serializer, someOtherKey);

        var result = await processor.Verify(request, null);
        Assert.False(result.IsValid);
        Assert.Equal("Invalid issuer signature for GridArea ”DK1”", result.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_NoIssuerForArea_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var someOtherKey = Key.Create(SignatureAlgorithm.Ed25519);

        var request = CreateRequest(serializer, someOtherKey, gridAreaOverride: "DK2");

        var result = await processor.Verify(request, null);
        Assert.False(result.IsValid);
        Assert.Equal("No issuer found for GridArea ”DK2”", result.ErrorMessage);
    }

    private ConsumptionIssuedRequest CreateRequest(
        JsonEventSerializer serializer,
        Key signerKey,
        CommitmentParameters? gsrnCommitmentOverride = null,
        CommitmentParameters? quantityCommitmentOverride = null,
        byte[]? ownerKeyOverride = null,
        string? gridAreaOverride = null
        )
    {
        var group = lazyGroup.Value;

        var quantityCommitmentParameters = group.Commit(150);
        var gsrnCommitmentParameters = group.Commit(5700000000000001);

        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var e = new ConsumptionIssuedEvent(
                new FederatedStreamId("registry", Guid.NewGuid()),
                new TimePeriod(
                    DateTimeOffset.Now,
                    DateTimeOffset.Now.AddHours(1)),
                gridAreaOverride ?? "DK1",
                gsrnCommitmentParameters.Commitment,
                quantityCommitmentParameters.Commitment,
                ownerKeyOverride ?? ownerKey.PublicKey.Export(KeyBlobFormat.RawPublicKey)
                );

        var serializedEvent = serializer.Serialize(e);
        var signature = NSec.Cryptography.Ed25519.Ed25519.Sign(signerKey, serializedEvent);

        var request = new ConsumptionIssuedRequest(
            GsrnParameters: gsrnCommitmentOverride ?? gsrnCommitmentParameters,
            QuantityParameters: quantityCommitmentOverride ?? quantityCommitmentParameters,
            Event: e,
            Signature: signature);

        return request;
    }
}
