using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption.Verifiers;
using ProjectOrigin.Electricity.Models;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionIssuedVerifierTests : AbstractVerifierTests
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
        var optionsMock = CreateOptionsMock(new IssuerOptions()
        {
            Issuers = new Dictionary<string, string>(){
                {"DK1", Convert.ToBase64String(issuerKey.PublicKey.Export(KeyBlobFormat.RawPublicKey))}
            }
        });

        var processor = new ConsumptionIssuedVerifier(optionsMock);

        return (processor, issuerKey);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_IssueCertificate_Success()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateConsumptionIssuedRequest(issuerKey);

        var result = await processor.Verify(request);

        AssertValid(result);
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_CertificateExists_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateConsumptionIssuedRequest(issuerKey, exists: true);

        var result = await processor.Verify(request);

        AssertInvalid(result, $"Certificate with id ”{request.Event.CertificateId.StreamId}” already exists");
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_QuantityCommitmentInvalid_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var request = FakeRegister.CreateConsumptionIssuedRequest(issuerKey, quantityCommitmentOverride: FakeRegister.InvalidCommitment());

        var result = await processor.Verify(request);

        AssertInvalid(result, "Invalid range proof for Quantity commitment");
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_InvalidOwner_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var randomOwnerKeyData = new V1.PublicKey
        {
            Content = Google.Protobuf.ByteString.CopyFrom(new Fixture().Create<byte[]>())
        };

        var request = FakeRegister.CreateConsumptionIssuedRequest(issuerKey, ownerKeyOverride: randomOwnerKeyData);

        var result = await processor.Verify(request);

        AssertInvalid(result, "Invalid owner key, not a valid Ed25519 publicKey");
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_InvalidSignature_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var someOtherKey = Key.Create(SignatureAlgorithm.Ed25519);

        var request = FakeRegister.CreateConsumptionIssuedRequest(someOtherKey);

        var result = await processor.Verify(request);

        AssertInvalid(result, "Invalid issuer signature for GridArea ”DK1”");
    }

    [Fact]
    public async Task ConsumptionIssuedVerifier_NoIssuerForArea_Fail()
    {
        var (processor, issuerKey) = SetupIssuer();

        var someOtherKey = Key.Create(SignatureAlgorithm.Ed25519);

        var request = FakeRegister.CreateConsumptionIssuedRequest(someOtherKey, gridAreaOverride: "DK2");

        var result = await processor.Verify(request);

        AssertInvalid(result, "No issuer found for GridArea ”DK2”");
    }
}
