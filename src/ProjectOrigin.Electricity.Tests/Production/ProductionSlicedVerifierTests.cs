using Google.Protobuf;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Production.Verifiers;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionSlicedVerifierTests : AbstractVerifierTest
{
    private IOptions<T> CreateOptionsMock<T>(T content) where T : class
    {
        var optionsMock = new Mock<IOptions<T>>();
        optionsMock.Setup(obj => obj.Value).Returns(content);
        return optionsMock.Object;
    }

    private Group Group { get => FakeRegister.Group; }

    private ProductionSlicedEventVerifier Verifier { get => new ProductionSlicedEventVerifier(); }

    [Fact]
    public async Task ProductionSlicedEventVerifier_TransferCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, ownerKey);

        var result = await Verifier.Verify(request);

        AssertValid(result);
    }

    [Fact]
    public async Task ProductionSlicedEventVerifier_NoCertificate_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, ownerKey, exists: false);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, $"Certificate does not exist");
    }

    [Fact]
    public async Task ProductionSlicedEventVerifier_FakeSlice_SliceNotFound()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var newOwnerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var fakeSliceParams = FakeRegister.Group.Commit(250);
        var request = FakeRegister.CreateSlices(cert, fakeSliceParams, 150, ownerKey);

        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Slice not found");
    }

    [Fact]
    public async Task ProductionSlicedEventVerifier_WrongKey_InvalidSignature()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var otherKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, otherKey);

        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Invalid signature for slice");
    }


    [Fact]
    public async Task ProductionSlicedEventVerifier_InvalidSlicePublicKey_InvalidFormat()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var randomOwnerKeyData = new V1.PublicKey
        {
            Content = Google.Protobuf.ByteString.CopyFrom(new Fixture().Create<byte[]>())
        };

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, ownerKey, randomOwnerKeyData);

        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Invalid NewOwner key, not a valid Ed25519 publicKey");
    }

    [Fact]
    public async Task ProductionSlicedEventVerifier_InvalidSumProof_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var sumOverride = ByteString.CopyFrom(new Fixture().Create<byte[]>());

        var request = FakeRegister.CreateSlices(cert, sourceParams, 150, ownerKey, sumOverride: sumOverride);

        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Invalid sum proof");
    }
}
