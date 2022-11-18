using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionIssuedVerifierTests
{
    private IOptions<T> CreateOptionsMock<T>(T content) where T : class
    {
        var optionsMock = new Mock<IOptions<T>>();
        optionsMock.Setup(obj => obj.Value).Returns(content);
        return optionsMock.Object;
    }

    private (ProductionIssuedVerifier, Key) SetupIssuer()
    {
        var issuerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var optionsMock = CreateOptionsMock(new IssuerOptions((area) => area == "DK1" ? issuerKey.PublicKey : null));

        var processor = new ProductionIssuedVerifier(optionsMock);

        return (processor, issuerKey);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_IssueCertificate_Success()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey);

        await processor.Verify(request, null);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_IssueCertificateWithPublicQuantity_Success()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey, publicQuantity: true);

        await processor.Verify(request, null);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_CertificateExists_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey);

        var result = await processor.Verify(request, new ProductionCertificate());

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal($"Certificate with id ”{request.FederatedStreamId.StreamId}” already exists", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_GsrnCommitmentInvalid_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey, gsrnCommitmentOverride: FakeRegister.Group.Commit(57682));

        var result = await processor.Verify(request, null);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Calculated GSRN commitment does not equal the parameters", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_QuantityCommitmentInvalid_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey, quantityCommitmentOverride: FakeRegister.Group.Commit(695956));

        var result = await processor.Verify(request, null);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Calculated Quantity commitment does not equal the parameters", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_InvalidPublicParameters_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey, publicQuantityCommitmentOverride: FakeRegister.Group.Commit(695956), publicQuantity: true);

        var result = await processor.Verify(request, null);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Private and public quantity proof does not match", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_InvalidOwner_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var randomOwnerKeyData = new Fixture().Create<byte[]>();
        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey, ownerKeyOverride: randomOwnerKeyData);

        var result = await processor.Verify(request, null);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Invalid owner key, not a valid Ed25519 publicKey", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_InvalidSignature_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var someOtherKey = Key.Create(SignatureAlgorithm.Ed25519);

        var request = FakeRegister.CreateProductionIssuedRequest(someOtherKey);

        var result = await processor.Verify(request, null);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Invalid issuer signature for GridArea ”DK1”", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_NoIssuerForArea_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var someOtherKey = Key.Create(SignatureAlgorithm.Ed25519);

        var request = FakeRegister.CreateProductionIssuedRequest(someOtherKey, gridAreaOverride: "DK2");

        var result = await processor.Verify(request, null);

        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("No issuer found for GridArea ”DK2”", invalid!.ErrorMessage);
    }
}
