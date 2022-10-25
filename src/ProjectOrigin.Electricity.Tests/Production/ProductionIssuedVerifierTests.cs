using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.RequestProcessor.Services;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionIssuedVerifierTests
{
    private IOptions<T> CreateOptionsMock<T>(T content) where T : class
    {
        var optionsMock = new Mock<IOptions<T>>();
        optionsMock.Setup(obj => obj.Value).Returns(content);
        return optionsMock.Object;
    }

    private (ProductionIssuedVerifier, JsonEventSerializer, Key) SetupIssuer()
    {
        var serializer = new JsonEventSerializer();
        var issuerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var optionsMock = CreateOptionsMock(new IssuerOptions((area) => area == "DK1" ? issuerKey.PublicKey : null));

        var processor = new ProductionIssuedVerifier(optionsMock, serializer);

        return (processor, serializer, issuerKey);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_IssueCertificate_Success()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(serializer, issuerKey);

        await processor.Verify(request, null);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_IssueCertificateWithPublicQuantity_Success()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();


        var request = FakeRegister.CreateProductionIssuedRequest(serializer, issuerKey, publicQuantity: true);

        await processor.Verify(request, null);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_CertificateExists_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(serializer, issuerKey);

        var result = await processor.Verify(request, new ProductionCertificate());
        Assert.False(result.IsValid);
        Assert.Equal($"Certificate with id ”{request.FederatedStreamId.StreamId}” already exists", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_GsrnCommitmentInvalid_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(serializer, issuerKey, gsrnCommitmentOverride: FakeRegister.Group.Commit(57682));

        var result = await processor.Verify(request, null);
        Assert.False(result.IsValid);
        Assert.Equal("Calculated GSRN commitment does not equal the parameters", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_QuantityCommitmentInvalid_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(serializer, issuerKey, quantityCommitmentOverride: FakeRegister.Group.Commit(695956));

        var result = await processor.Verify(request, null);
        Assert.False(result.IsValid);
        Assert.Equal("Calculated Quantity commitment does not equal the parameters", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_InvalidPublicParameters_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(serializer, issuerKey, publicQuantityCommitmentOverride: FakeRegister.Group.Commit(695956), publicQuantity: true);

        var result = await processor.Verify(request, null);
        Assert.False(result.IsValid);
        Assert.Equal("QuantityParameters and QuantityCommitmentParameters are not the same", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_InvalidOwner_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var randomOwnerKeyData = new Fixture().Create<byte[]>();
        var request = FakeRegister.CreateProductionIssuedRequest(serializer, issuerKey, ownerKeyOverride: randomOwnerKeyData);

        var result = await processor.Verify(request, null);
        Assert.False(result.IsValid);
        Assert.Equal("Invalid owner key, not a valid Ed25519 publicKey", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_InvalidSignature_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var someOtherKey = Key.Create(SignatureAlgorithm.Ed25519);

        var request = FakeRegister.CreateProductionIssuedRequest(serializer, someOtherKey);

        var result = await processor.Verify(request, null);
        Assert.False(result.IsValid);
        Assert.Equal("Invalid issuer signature for GridArea ”DK1”", result.ErrorMessage);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_NoIssuerForArea_Fail()
    {
        var (processor, serializer, issuerKey) = SetupIssuer();

        var someOtherKey = Key.Create(SignatureAlgorithm.Ed25519);

        var request = FakeRegister.CreateProductionIssuedRequest(serializer, someOtherKey, gridAreaOverride: "DK2");

        var result = await processor.Verify(request, null);
        Assert.False(result.IsValid);
        Assert.Equal("No issuer found for GridArea ”DK2”", result.ErrorMessage);
    }
}
