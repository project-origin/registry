using Google.Protobuf;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Production.Verifiers;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionSlicedVerifierTests : AbstractVerifierTests
{
    private IOptions<T> CreateOptionsMock<T>(T content) where T : class
    {
        var optionsMock = new Mock<IOptions<T>>();
        optionsMock.Setup(obj => obj.Value).Returns(content);
        return optionsMock.Object;
    }

    private ConsumptionSlicedVerifier Verifier { get => new ConsumptionSlicedVerifier(); }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_TransferCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, ownerKey);

        var result = await Verifier.Verify(request);

        AssertValid(result);
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_NoCertificate_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, ownerKey, exists: false);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Certificate does not exist");
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_FakeSlice_SliceNotFound()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var newOwnerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var fakeSliceParams = new SecretCommitmentInfo(250);
        var request = FakeRegister.CreateSlices(cert, fakeSliceParams, 150, ownerKey);

        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Slice not found");
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_WrongKey_InvalidSignature()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var otherKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, otherKey);

        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Invalid signature for slice");
    }


    [Fact]
    public async Task ConsumptionSlicedEventVerifier_InvalidSlicePublicKey_InvalidFormat()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var randomOwnerKeyData = new V1.PublicKey
        {
            Content = Google.Protobuf.ByteString.CopyFrom(new Fixture().Create<byte[]>())
        };

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, ownerKey, randomOwnerKeyData);

        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Invalid NewOwner key, not a valid Ed25519 publicKey");
    }

    [Fact]
    public async Task ConsumptionSlicedEventVerifier_InvalidSumProof_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ConsumptionIssued(ownerKey.PublicKey, 250);

        var sumOverride = ByteString.CopyFrom(new Fixture().CreateMany<byte>(64).ToArray());

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, ownerKey, sumOverride: sumOverride);

        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Invalid sum proof");
    }
}
