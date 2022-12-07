using NSec.Cryptography;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionTransferredVerifierTests
{
    private ProductionTransferOwnershipEventVerifier Verifier { get => new ProductionTransferOwnershipEventVerifier(); }

    [Fact]
    public async Task ProductionTransferredVerifierTests_TransferCertificate_Valid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var newOwnerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert, sourceParams, newOwnerKey.PublicKey.ToProto(), ownerKey);

        var result = await Verifier.Verify(request);

        Assert.IsType<VerificationResult.Valid>(result);
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_NullCertificate_Invalid()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var newOwnerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert, sourceParams, newOwnerKey.PublicKey.ToProto(), ownerKey);
        var modifiedRequest = new VerificationRequest<ProductionCertificate, V1.TransferredEvent>(
             null,
             request.Event,
             request.Signature,
             request.AdditionalStreams
         );

        var result = await Verifier.Verify(modifiedRequest);

        var invalid = Assert.IsType<VerificationResult.Invalid>(result);
        Assert.Equal("Certificate does not exist", invalid!.ErrorMessage);
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

        var invalid = Assert.IsType<VerificationResult.Invalid>(result);
        Assert.Equal("Invalid NewOwner key, not a valid Ed25519 publicKey", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_FakeSlice_SliceNotFound()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var newOwnerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var fakeSliceParams = FakeRegister.Group.Commit(250);
        var request = FakeRegister.CreateTransfer(cert, fakeSliceParams, newOwnerKey.PublicKey.ToProto(), ownerKey);

        var result = await Verifier.Verify(request);

        var invalid = Assert.IsType<VerificationResult.Invalid>(result);
        Assert.Equal("Slice not found", invalid!.ErrorMessage);
    }

    [Fact]
    public async Task ProductionTransferredVerifierTests_WrongKey_InvalidSignature()
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var newOwnerKey = Key.Create(SignatureAlgorithm.Ed25519);
        var (cert, sourceParams) = FakeRegister.ProductionIssued(ownerKey.PublicKey, 250);

        var request = FakeRegister.CreateTransfer(cert, sourceParams, newOwnerKey.PublicKey.ToProto(), newOwnerKey);

        var result = await Verifier.Verify(request);

        var invalid = Assert.IsType<VerificationResult.Invalid>(result);
        Assert.Equal("Invalid signature for slice", invalid!.ErrorMessage);
    }
}
