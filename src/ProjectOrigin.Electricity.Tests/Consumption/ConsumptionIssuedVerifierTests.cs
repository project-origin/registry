using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Consumption.Requests;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Register.LineProcessor.Models;
using ProjectOrigin.Register.LineProcessor.Services;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionIssuedVerifierTests
{
    private IOptions<T> CreateOptionsMock<T>(T content) where T : class
    {
        var optionsMock = new Mock<IOptions<T>>();
        optionsMock.Setup(obj => obj.Value).Returns(content);
        return optionsMock.Object;
    }

    private (ConsumptionIssuedVerifier, Key) SetupIssuer()
    {
        var issuerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var optionsMock = CreateOptionsMock(new IssuerOptions((area) => area == "DK1" ? issuerKey.PublicKey : null));

        var processor = new ConsumptionIssuedVerifier(optionsMock);

        return (processor, issuerKey);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_IssueCertificate_Success()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateConsumptionIssuedRequest(issuerKey);

        await processor.Verify(request, null);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_CertificateExists_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateConsumptionIssuedRequest(issuerKey);

        var result = await processor.Verify(request, new ConsumptionCertificate());
        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal($"Certificate with id ”{request.FederatedStreamId.StreamId}” already exists", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_GsrnCommitmentInvalid_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateConsumptionIssuedRequest(issuerKey, gsrnCommitmentOverride: FakeRegister.Group.Commit(57682));

        var result = await processor.Verify(request, null);
        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Calculated GSRN commitment does not equal the parameters", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_QuantityCommitmentInvalid_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateConsumptionIssuedRequest(issuerKey, quantityCommitmentOverride: FakeRegister.Group.Commit(695956));

        var result = await processor.Verify(request, null);
        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Calculated Quantity commitment does not equal the parameters", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_InvalidOwner_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var randomOwnerKeyData = new Fixture().Create<byte[]>();
        var request = FakeRegister.CreateConsumptionIssuedRequest(issuerKey, ownerKeyOverride: randomOwnerKeyData);

        var result = await processor.Verify(request, null);
        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Invalid owner key, not a valid Ed25519 publicKey", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_InvalidSignature_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var someOtherKey = Key.Create(SignatureAlgorithm.Ed25519);

        var request = FakeRegister.CreateConsumptionIssuedRequest(someOtherKey);

        var result = await processor.Verify(request, null);
        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("Invalid issuer signature for GridArea ”DK1”", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_NoIssuerForArea_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var someOtherKey = Key.Create(SignatureAlgorithm.Ed25519);

        var request = FakeRegister.CreateConsumptionIssuedRequest(someOtherKey, gridAreaOverride: "DK2");

        var result = await processor.Verify(request, null);
        var invalid = result as VerificationResult.Invalid;
        Assert.NotNull(invalid);
        Assert.Equal("No issuer found for GridArea ”DK2”", invalid!.ErrorMessage);
    }
}
