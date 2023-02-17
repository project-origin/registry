using NSec.Cryptography;
using ProjectOrigin.Electricity.Production.Verifiers;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionTransferredVerifierTests : AbstractVerifierTests
{
    private ProductionTransferredVerifier Verifier { get => new ProductionTransferredVerifier(); }

    [Fact]
    public async Task ProductionTransferredVerifierTests_TransferCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var newOwnerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert, sourceParams, newOwnerKey.PublicKey.ToProto(), ownerKey);

        var result = await Verifier.Verify(request);

        AssertValid(result);
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_NullCertificate_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var newOwnerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert, sourceParams, newOwnerKey.PublicKey.ToProto(), ownerKey, exists: false);
        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Certificate does not exist");
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_InvalidPublicKey_InvalidFormat()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var newOwnerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var randomOwnerKeyData = new V1.PublicKey
        {
            Content = Google.Protobuf.ByteString.CopyFrom(new Fixture().Create<byte[]>())
        };

        var request = FakeRegister.CreateTransfer(cert, sourceParams, randomOwnerKeyData, ownerKey);

        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Invalid NewOwner key, not a valid Ed25519 publicKey");
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_FakeSlice_SliceNotFound()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var newOwnerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var fakeSliceParams = new SecretCommitmentInfo(250);
        var request = FakeRegister.CreateTransfer(cert, fakeSliceParams, newOwnerKey.PublicKey.ToProto(), ownerKey);

        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Slice not found");
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_WrongKey_InvalidSignature()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var newOwnerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert, sourceParams, newOwnerKey.PublicKey.ToProto(), newOwnerKey);

        var result = await Verifier.Verify(request);

        AssertInvalid(result, "Invalid signature for slice");
    }
}
