using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Production.Verifiers;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionIssuedVerifierTests : AbstractVerifierTests
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
        var optionsMock = CreateOptionsMock(new IssuerOptions()
        {
            AreaIssuerPublicKey = (area) => area == "DK1" ? issuerKey.PublicKey : null
        });

        var processor = new ProductionIssuedVerifier(optionsMock);

        return (processor, issuerKey);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_IssueCertificate_Success()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey);

        var result = await processor.Verify(request);

        AssertValid(result);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_IssueCertificateWithPublicQuantity_Success()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey, publicQuantity: true);

        var result = await processor.Verify(request);

        AssertValid(result);
    }

    [Fact]
    public async Task ProductionIssuedVerifier_CertificateExists_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey, exists: true);
        var result = await processor.Verify(request);

        AssertInvalid(result, $"Certificate with id ”{request.Event.CertificateId.StreamId}” already exists");
    }

    [Fact]
    public async Task ProductionIssuedVerifier_QuantityCommitmentInvalid_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey, quantityCommitmentOverride: FakeRegister.InvalidCommitment());

        var result = await processor.Verify(request);

        AssertInvalid(result, "Invalid range proof for Quantity commitment");
    }

    [Fact]
    public async Task ProductionIssuedVerifier_InvalidPublicParameters_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey, publicQuantityCommitmentOverride: new SecretCommitmentInfo(695956), publicQuantity: true);

        var result = await processor.Verify(request);

        AssertInvalid(result, "Private and public quantity proof does not match");
    }

    [Fact]
    public async Task ProductionIssuedVerifier_InvalidOwner_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var randomOwnerKeyData = new V1.PublicKey
        {
            Content = Google.Protobuf.ByteString.CopyFrom(new Fixture().Create<byte[]>())
        };

        var request = FakeRegister.CreateProductionIssuedRequest(issuerKey, ownerKeyOverride: randomOwnerKeyData);

        var result = await processor.Verify(request);

        AssertInvalid(result, "Invalid owner key, not a valid Ed25519 publicKey");
    }

    [Fact]
    public async Task ProductionIssuedVerifier_InvalidSignature_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var someOtherKey = Key.Create(SignatureAlgorithm.Ed25519);

        var request = FakeRegister.CreateProductionIssuedRequest(someOtherKey);

        var result = await processor.Verify(request);

        AssertInvalid(result, "Invalid issuer signature for GridArea ”DK1”");
    }

    [Fact]
    public async Task ProductionIssuedVerifier_NoIssuerForArea_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var someOtherKey = Key.Create(SignatureAlgorithm.Ed25519);

        var request = FakeRegister.CreateProductionIssuedRequest(someOtherKey, gridAreaOverride: "DK2");

        var result = await processor.Verify(request);

        AssertInvalid(result, "No issuer found for GridArea ”DK2”");
    }
}
